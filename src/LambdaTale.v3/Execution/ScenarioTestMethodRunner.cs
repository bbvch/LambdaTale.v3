using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Execution;

public class
    ScenarioTestMethodRunner : TestMethodRunner<ScenarioTestMethodRunnerContext, ScenarioTestMethod, ScenarioTestCase>
{
    public static ScenarioTestMethodRunner Instance { get; } = new();

    public async ValueTask<RunSummary> Run(
        ScenarioTestMethod testMethod,
        IReadOnlyCollection<ScenarioTestCase> testCases,
        IMessageBus messageBus,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        await using var ctxt = new ScenarioTestMethodRunnerContext(testMethod,  testCases, messageBus,
            aggregator,
            cancellationTokenSource);
        await ctxt.InitializeAsync();

        return await this.Run(ctxt);
    }

    protected override ValueTask<bool> OnTestMethodStarting(ScenarioTestMethodRunnerContext ctxt)
    {
        Guard.ArgumentNotNull(ctxt);

        return new(ctxt.MessageBus.QueueMessage(new TestMethodStarting
        {
            AssemblyUniqueID = ctxt.TestMethod.TestClass.TestCollection.TestAssembly.UniqueID,
            MethodArity = ctxt.TestMethod.MethodArity,
            MethodName = Guard.ArgumentNotNull(ctxt).TestMethod.MethodName,
            TestClassUniqueID = ctxt.TestMethod.TestClass.UniqueID,
            TestCollectionUniqueID = ctxt.TestMethod.TestClass.TestCollection.UniqueID,
            TestMethodUniqueID = ctxt.TestMethod.UniqueID,
            Traits = ctxt.TestMethod.Traits,
        }));
    }

    protected override ValueTask<RunSummary> RunTestCase(
        ScenarioTestMethodRunnerContext ctxt,
        ScenarioTestCase testCase) =>
        ScenarioTestCaseRunner.Instance.Run(testCase, ctxt.MessageBus, ctxt.Aggregator.Clone(),
            ctxt.CancellationTokenSource);
}

public class ScenarioTestMethodRunnerContext(
    ScenarioTestMethod testMethod,
    IReadOnlyCollection<ScenarioTestCase> testCases,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource) :
    TestMethodRunnerContext<ScenarioTestMethod, ScenarioTestCase>(testMethod, testCases, ExplicitOption.Off, messageBus,
        aggregator, cancellationTokenSource)
{
}
