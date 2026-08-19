using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

internal sealed class ScenarioStepRunnerContext(
    ScenarioStep step,
    ScenarioTestCaseRunnerContext caseContext,
    TestOutputHelper outputHelper,
    Func<ValueTask> body,
    string? skipReason = null,
    TimeSpan? elapsedOverride = null)
    : TestRunnerBaseContext<ScenarioStep>(
        step,
        caseContext.MessageBus,
        skipReason,
        caseContext.ExplicitOption,
        new ExceptionAggregator(),
        caseContext.CancellationTokenSource)
{
    public TestOutputHelper OutputHelper => outputHelper;

    public bool Explicit => caseContext.Explicit;

    public int Timeout => caseContext.TestCase.Timeout;

    // TestRunnerBase discards the elapsed time of a RunTest that throws, so a failing body has to
    // reach the aggregator rather than the caller.
    public async ValueTask<TimeSpan> RunBody()
    {
        var measured = await ExecutionTimer.MeasureAsync(() => this.Aggregator.RunAsync(body));
        return elapsedOverride ?? measured;
    }

    // LambdaTale treats any exception assignable to a declared skip type as a skip, where xunit's
    // own contexts require an exact type match.
    public override string? GetSkipReason(Exception? exception) =>
        exception is not null
        && caseContext.TestCase.SkipExceptions is { } types
        && types.Any(t => t.IsInstanceOfType(exception))
            ? exception.Message
            : base.GetSkipReason(exception);
}

internal sealed class ScenarioStepRunner : TestRunnerBase<ScenarioStepRunnerContext, ScenarioStep>
{
    public static ScenarioStepRunner Instance { get; } = new();

    public async ValueTask<RunSummary> RunStep(ScenarioStepRunnerContext ctxt)
    {
        await using (ctxt)
        {
            return await this.Run(ctxt);
        }
    }

    protected override ValueTask<IReadOnlyDictionary<string, TestAttachment>?> GetAttachments(ScenarioStepRunnerContext ctxt) =>
        new(TestContext.Current.Attachments);

    protected override ValueTask<string> GetTestOutput(ScenarioStepRunnerContext ctxt) =>
        new(ctxt.OutputHelper.Output);

    protected override ValueTask<string[]?> GetWarnings(ScenarioStepRunnerContext ctxt) =>
        new(TestContext.Current.Warnings?.ToArray());

    protected override ValueTask<bool> OnTestStarting(ScenarioStepRunnerContext ctxt) =>
        this.OnTestStarting(ctxt, ctxt.Explicit, ctxt.Timeout);

    protected override ValueTask<TimeSpan> RunTest(ScenarioStepRunnerContext ctxt) => ctxt.RunBody();

    protected override void SetTestContext(
        ScenarioStepRunnerContext ctxt,
        TestEngineStatus testStatus,
        TestResultState? testState = null,
        object? testClassInstance = null) =>
        TestContext.SetForTest(
            ctxt.Test,
            testStatus,
            ctxt.CancellationTokenSource.Token,
            testState,
            ctxt.OutputHelper,
            testClassInstance);
}
