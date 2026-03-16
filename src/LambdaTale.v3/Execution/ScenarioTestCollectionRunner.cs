using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Execution;

public class ScenarioTestCollectionRunner :
    TestCollectionRunner<ScenarioTestCollectionRunnerContext, ScenarioTestCollection, ScenarioTestClass,
        ScenarioTestCase>
{
    public static ScenarioTestCollectionRunner Instance { get; } = new();

    protected override async ValueTask<RunSummary> RunTestClass(
        ScenarioTestCollectionRunnerContext ctxt,
        ScenarioTestClass? testClass,
        IReadOnlyCollection<ScenarioTestCase> testCases)
    {
        ArgumentNullException.ThrowIfNull(testClass);

        return await ScenarioTestClassRunner.Instance.Run(testClass, testCases, ctxt.MessageBus,
            ctxt.Aggregator.Clone(), ctxt.CancellationTokenSource);
    }

    public async ValueTask<RunSummary> Run(
        ScenarioTestCollection testCollection,
        IReadOnlyCollection<ScenarioTestCase> testCases,
        IMessageBus messageBus,
        ExceptionAggregator exceptionAggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        await using var ctxt = new ScenarioTestCollectionRunnerContext(testCollection, testCases, messageBus,
            exceptionAggregator, cancellationTokenSource);
        await ctxt.InitializeAsync();

        return await this.Run(ctxt);
    }
}

public class ScenarioTestCollectionRunnerContext(
    ScenarioTestCollection testCollection,
    IReadOnlyCollection<ScenarioTestCase> testCases,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource) :
    TestCollectionRunnerContext<ScenarioTestCollection, ScenarioTestCase>(testCollection, testCases,
        ExplicitOption.Off, messageBus, aggregator, cancellationTokenSource)
{
}
