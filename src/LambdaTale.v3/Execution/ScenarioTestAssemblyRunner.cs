using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Execution;

public class ScenarioTestAssemblyRunner :
    TestAssemblyRunner<ScenarioTestAssemblyRunnerContext, ScenarioTestAssembly, ScenarioTestCollection,
        ScenarioTestCase>
{
    public static ScenarioTestAssemblyRunner Instance { get; } = new();

    protected override ValueTask<string> GetTestFrameworkDisplayName(ScenarioTestAssemblyRunnerContext ctxt) =>
        new("LambdaTale.v3");

    public async ValueTask<RunSummary> Run(
        ScenarioTestAssembly testAssembly,
        IReadOnlyCollection<ScenarioTestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        CancellationToken cancellationToken)
    {
        await using var ctxt = new ScenarioTestAssemblyRunnerContext(testAssembly, testCases, executionMessageSink,
            executionOptions, cancellationToken);
        await ctxt.InitializeAsync();

        return await this.Run(ctxt);
    }

    protected override ValueTask<RunSummary> RunTestCollection(
        ScenarioTestAssemblyRunnerContext ctxt,
        ScenarioTestCollection testCollection,
        IReadOnlyCollection<ScenarioTestCase> testCases) =>
        ScenarioTestCollectionRunner.Instance.Run(
            testCollection,
            testCases,
            ctxt.MessageBus,
            ctxt.Aggregator.Clone(),
            ctxt.CancellationTokenSource);
}

public class ScenarioTestAssemblyRunnerContext(
    ScenarioTestAssembly testAssembly,
    IReadOnlyCollection<ScenarioTestCase> testCases,
    IMessageSink executionMessageSink,
    ITestFrameworkExecutionOptions executionOptions,
    CancellationToken cancellationToken) :
    TestAssemblyRunnerContext<ScenarioTestAssembly, ScenarioTestCase>(testAssembly, testCases, executionMessageSink,
        executionOptions, cancellationToken)
{
}
