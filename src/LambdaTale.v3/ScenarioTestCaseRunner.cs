using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

internal sealed class ScenarioTestCaseRunnerContext(
    ScenarioTestCase testCase,
    ExplicitOption explicitOption,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource,
    object?[] constructorArguments,
    string? skipReason)
    : TestCaseRunnerBaseContext<ScenarioTestCase>(testCase, explicitOption, messageBus, aggregator, cancellationTokenSource)
{
    private int nextTestIndex;

    public object?[] ConstructorArguments => constructorArguments;

    // Computed once per case run rather than per step: ITestCaseMetadata.Traits builds a fresh
    // dictionary on every access.
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits { get; } = ((ITestCaseMetadata)testCase).Traits;

    // Every step of a case is reported as its own test, so each needs a distinct index. The index
    // in a step's display name restarts for each data row; this one must not, or the steps of two
    // rows of a delay-enumerated case would share unique IDs.
    // Interlocked because a timed-out case reports its (Timeout) step while the scenario it gave
    // up on is still producing steps of its own.
    public int NextTestIndex() => Interlocked.Increment(ref this.nextTestIndex) - 1;

    // The static reason merged with the conditional and explicit-option ones, resolved before the
    // run starts so a malformed [Scenario(SkipUnless = ...)] still surfaces to the caller.
    public string? SkipReason => skipReason;
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
        await using var ctxt = new ScenarioTestCaseRunnerContext(
            testCase, explicitOption, messageBus, aggregator, cancellationTokenSource, constructorArguments, skipReason);
        await ctxt.InitializeAsync();
        return await Instance.Run(ctxt);
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

        return await ScenarioCaseRunner.RunSyntheticStep(
            ctxt,
            "(Timeout)",
            new TimeoutException($"Test exceeded timeout of {timeout}ms"),
            TimeSpan.FromMilliseconds(timeout));
    }
}
