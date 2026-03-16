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

        // Invoke the scenario method once to collect live step definitions.
        // Done here (not in discovery) so lambdas are fresh per execution
        // and re-runs after deserialization work correctly.
        var stepDefinitionsByRow = new Dictionary<int, Dictionary<int, ScenarioTestDefinition>>();
        foreach (var group in testCases.GroupBy(tc => tc.DataRowIndex))
        {
            var args = group.First().TestMethodArguments;
            var parameterInfos = testMethod.Method.GetParameters();
            var parameterValues = args ?? new object?[parameterInfos.Length];
            if (args is null)
                for (var i = 0; i < parameterInfos.Length; i++)
                    parameterValues[i] = parameterInfos[i].ParameterType.GetDefaultValue();

            using var scenarioCtx = Scenario.Acquire();
            testMethod.Method.Invoke(classInstance, parameterValues);
            stepDefinitionsByRow[group.Key] = Scenario.TestDefinitions.ToDictionary(td => td.index);
        }

        await using var ctxt = new ScenarioTestMethodRunnerContext(
            testMethod, classInstance, testCases, stepDefinitionsByRow,
            messageBus, aggregator, cancellationTokenSource);
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
        ScenarioTestCase testCase)
    {
        if (!ctxt.StepDefinitionsByRow.TryGetValue(testCase.DataRowIndex, out var rowDefs) ||
            !rowDefs.TryGetValue(testCase.CaseIndex, out var stepDef))
            throw new InvalidOperationException(
                $"No step definition found for row {testCase.DataRowIndex}, index {testCase.CaseIndex} " +
                $"in scenario '{ctxt.TestMethod.MethodName}'. " +
                $"This can happen if the scenario method body has changed since discovery.");

        var step = new ScenarioStep(testCase, stepDef.Tale, stepDef.Lambda);

        return ScenarioTestCaseRunner.Instance.Run(
            testCase, step, ctxt.ScenarioClass, ctxt.MessageBus,
            ctxt.Aggregator.Clone(), ctxt.CancellationTokenSource);
    }
}

public class ScenarioTestMethodRunnerContext(
    ScenarioTestMethod testMethod,
    object testClassInstance,
    IReadOnlyCollection<ScenarioTestCase> testCases,
    IReadOnlyDictionary<int, Dictionary<int, ScenarioTestDefinition>> stepDefinitionsByRow,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource) :
    TestMethodRunnerContext<ScenarioTestMethod, ScenarioTestCase>(testMethod, testCases, ExplicitOption.Off, messageBus,
        aggregator, cancellationTokenSource)
{
    public object ScenarioClass { get; } = testClassInstance;
    public IReadOnlyDictionary<int, Dictionary<int, ScenarioTestDefinition>> StepDefinitionsByRow { get; } = stepDefinitionsByRow;
}
