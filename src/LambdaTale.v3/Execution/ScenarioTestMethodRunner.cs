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
        object? classInstance,
        IReadOnlyCollection<ScenarioTestCase> testCases,
        IMessageBus messageBus,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        Guard.ArgumentNotNull(classInstance);
        // TODO: db:
        //  - ? Should the method be `run` here before executing the lambda bodies to `setup` the scenario?
        //  - ? What happens about possible `[Background]` and `[Teardown]` steps
        //  - ? Is additional context needed on the testcase itself
        await using var ctxt = new ScenarioTestMethodRunnerContext(testMethod, classInstance, testCases, messageBus,
            aggregator,
            cancellationTokenSource);
        await ctxt.InitializeAsync();

        return await this.Run(ctxt);
    }

    // TODO: db: Is this needed?
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
        ScenarioTestCaseRunner.Instance.Run(testCase, ctxt.ScenarioClass, ctxt.MessageBus, ctxt.Aggregator.Clone(),
            ctxt.CancellationTokenSource);
}

public class ScenarioTestMethodRunnerContext(
    ScenarioTestMethod testMethod,
    object testClassInstance,
    IReadOnlyCollection<ScenarioTestCase> testCases,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource) :
    TestMethodRunnerContext<ScenarioTestMethod, ScenarioTestCase>(testMethod, testCases, ExplicitOption.Off, messageBus,
        aggregator, cancellationTokenSource)
{
    public object ScenarioClass { get; } = testClassInstance;
}
