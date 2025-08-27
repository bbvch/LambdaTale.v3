using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Execution;

public class ScenarioTestCaseRunner : TestCaseRunner<ScenarioTestCaseRunnerContext, ScenarioTestCase, ScenarioStep>
{
    public static ScenarioTestCaseRunner Instance { get; } = new();

    public async ValueTask<RunSummary> Run(
        ScenarioTestCase testCase,
        IMessageBus messageBus,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        await using var ctxt = new ScenarioTestCaseRunnerContext(testCase, messageBus, aggregator,
            cancellationTokenSource);
        await ctxt.InitializeAsync();

        return await this.Run(ctxt);
    }

    protected override ValueTask<RunSummary> RunTest(ScenarioTestCaseRunnerContext ctxt, ScenarioStep test) =>
        ScenarioStepRunner.Instance.Run(test, ctxt.MessageBus, null, ctxt.Aggregator.Clone(),
            ctxt.CancellationTokenSource);
}

public class ScenarioTestCaseRunnerContext(
    ScenarioTestCase testCase,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource) :
    TestCaseRunnerContext<ScenarioTestCase, ScenarioStep>(testCase, ExplicitOption.Off, messageBus, aggregator,
        cancellationTokenSource)
{
    public override IReadOnlyCollection<ScenarioStep> Tests =>
        [new ScenarioStep(this.TestCase, null)];
}
