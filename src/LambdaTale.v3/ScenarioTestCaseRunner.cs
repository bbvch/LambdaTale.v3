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
    public object?[] ConstructorArguments => constructorArguments;

    // The static reason merged with the conditional and explicit-option ones, resolved before the
    // run starts so a malformed [Scenario(SkipUnless = ...)] still surfaces to the caller.
    public string? SkipReason => skipReason;

    public bool Explicit => ((ITestCaseMetadata)this.TestCase).Explicit;
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
            return await ScenarioCaseRunner.RunSyntheticStep(ctxt, "(Startup)", stepIndex: 0, exception, TimeSpan.Zero);
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
        if (timeout <= 0 || await Task.WhenAny(dispatch, Task.Delay(timeout)) == dispatch)
        {
            return await dispatch;
        }

        return await ScenarioCaseRunner.RunSyntheticStep(
            ctxt,
            "(Timeout)",
            stepIndex: 0,
            new TimeoutException($"Test exceeded timeout of {timeout}ms"),
            TimeSpan.FromMilliseconds(timeout));
    }
}
