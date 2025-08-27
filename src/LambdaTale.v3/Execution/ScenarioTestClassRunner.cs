using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Execution;

public class ScenarioTestClassRunner :
    TestClassRunner<ScenarioTestClassRunnerContext, ScenarioTestClass, ScenarioTestMethod, ScenarioTestCase>
{
    public static ScenarioTestClassRunner Instance { get; } = new();

    public async ValueTask<RunSummary> Run(
        ScenarioTestClass testClass,
        IReadOnlyCollection<ScenarioTestCase> testCases,
        IMessageBus messageBus,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        await using var ctxt =
            new ScenarioTestClassRunnerContext(testClass, testCases, messageBus, aggregator, cancellationTokenSource);
        await ctxt.InitializeAsync();

        return await this.Run(ctxt);
    }

    protected override ValueTask<RunSummary> RunTestMethod(
        ScenarioTestClassRunnerContext ctxt,
        ScenarioTestMethod? testMethod,
        IReadOnlyCollection<ScenarioTestCase> testCases,
        object?[] constructorArguments)
    {
        ArgumentNullException.ThrowIfNull(testMethod);

        return ScenarioTestMethodRunner.Instance.Run(testMethod, testCases, ctxt.MessageBus, ctxt.Aggregator.Clone(),
            ctxt.CancellationTokenSource);
    }
}

public class ScenarioTestClassRunnerContext(
    ScenarioTestClass testClass,
    IReadOnlyCollection<ScenarioTestCase> testCases,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource) :
    TestClassRunnerContext<ScenarioTestClass, ScenarioTestCase>(testClass, testCases, ExplicitOption.Off, messageBus,
        aggregator, cancellationTokenSource)
{
}
