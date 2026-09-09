using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

internal sealed class ScenarioTestCaseRunnerContext(
    ScenarioTestCase testCase,
    ExplicitOption explicitOption,
    StoppableMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource caseCancellation,
    CancellationTokenSource timeoutCancellation,
    object?[] constructorArguments,
    string? skipReason)
    : TestCaseRunnerBaseContext<ScenarioTestCase>(testCase, explicitOption, messageBus, aggregator, caseCancellation)
{
    private int nextSyntheticTestIndex;
    private volatile bool timedOut;

    public object?[] ConstructorArguments => constructorArguments;

    // Computed once per case run rather than per step: ITestCaseMetadata.Traits builds a fresh
    // dictionary on every access.
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits { get; } = ((ITestCaseMetadata)testCase).Traits;

    // Every step of a case is reported as its own test, so each needs a distinct index. The index
    // in a step's display name restarts for each data row; this one must not, or the steps of two
    // rows of a delay-enumerated case would share unique IDs.
    // Negative so it cannot collide with the zero-based display index real steps derive theirs from.
    // Interlocked because a timed-out case reports its (Timeout) step while the scenario it gave
    // up on is still producing steps of its own.
    public int NextSyntheticTestIndex() => -Interlocked.Increment(ref this.nextSyntheticTestIndex);

    // The static reason merged with the conditional and explicit-option ones, resolved before the
    // run starts so a malformed [Scenario(SkipUnless = ...)] still surfaces to the caller.
    public string? SkipReason => skipReason;

    public bool HasTimedOut => this.timedOut;

    public void SignalTimeout() => this.timedOut = true;

    // Only reclaims a step that awaits the token it was handed; one that ignores it runs to the end.
    public void CancelRunawayWork() => timeoutCancellation.Cancel();

    public void StopReporting() => messageBus.Stop();
}

internal sealed class ScenarioTestCaseRunner : TestCaseRunnerBase<ScenarioTestCaseRunnerContext, ScenarioTestCase>
{
    private static readonly ScenarioTestCaseRunner Instance = new();

    public static async ValueTask<RunSummary> RunCase(
        ScenarioTestCase testCase,
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        object?[] constructorArguments,
        string? skipReason)
    {
        // Linked so a timeout cancels only this case rather than the whole run.
        var timeoutCancellation = new CancellationTokenSource();
        var caseCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationTokenSource.Token, timeoutCancellation.Token);
        var disposeCaseCancellation = true;

        try
        {
            await using var ctxt = new ScenarioTestCaseRunnerContext(
                testCase,
                explicitOption,
                new StoppableMessageBus(messageBus),
                aggregator,
                caseCancellation,
                timeoutCancellation,
                constructorArguments,
                skipReason);
            await ctxt.InitializeAsync();
            var summary = await Instance.Run(ctxt);

            if (caseCancellation.IsCancellationRequested
                && !timeoutCancellation.IsCancellationRequested
                && !cancellationTokenSource.IsCancellationRequested)
            {
                await cancellationTokenSource.CancelAsync();
            }

            // The abandoned step still holds these tokens; disposing them would turn a clean
            // cancellation into an ObjectDisposedException.
            disposeCaseCancellation = !ctxt.HasTimedOut;
            return summary;
        }
        finally
        {
            if (disposeCaseCancellation)
            {
                caseCancellation.Dispose();
                timeoutCancellation.Dispose();
            }
        }
    }

    protected override async ValueTask<RunSummary> RunTestCase(ScenarioTestCaseRunnerContext ctxt, Exception? exception)
    {
        if (exception is not null)
        {
            return await ScenarioCaseRunner.RunSyntheticStep(ctxt, "(Startup)", exception, TimeSpan.Zero);
        }

        if (ctxt.SkipReason is not null)
        {
            return await ScenarioCaseRunner.RunSkippedCase(ctxt, ctxt.SkipReason);
        }

        var testCase = ctxt.TestCase;
        var dispatch = testCase.IsDelayEnumerated
            ? ScenarioCaseRunner.RunDelayEnumerated(ctxt).AsTask()
            : ScenarioCaseRunner.RunWithArguments(ctxt, testCase.TestMethodArguments).AsTask();

        var timeout = testCase.Timeout;
        if (timeout <= 0)
        {
            return await dispatch;
        }

        using var timer = new CancellationTokenSource();
        var expired = Task.Delay(timeout, timer.Token);
        var completed = await Task.WhenAny(dispatch, expired);

        // Without this the timer outlives a scenario that finished quickly, keeping a pending
        // callback alive for the rest of the timeout.
        await timer.CancelAsync();

        if (completed == dispatch)
        {
            return await dispatch;
        }

        // Order matters: signal first so no further step starts, report while the bus is still
        // live, then close it so the in-flight step's unwinding goes unreported.
        ctxt.SignalTimeout();

        var timedOutSummary = await ScenarioCaseRunner.RunSyntheticStep(
            ctxt,
            "(Timeout)",
            new TimeoutException($"Test exceeded timeout of {timeout}ms"),
            TimeSpan.FromMilliseconds(timeout));

        ctxt.CancelRunawayWork();
        ctxt.StopReporting();

        return timedOutSummary;
    }
}
