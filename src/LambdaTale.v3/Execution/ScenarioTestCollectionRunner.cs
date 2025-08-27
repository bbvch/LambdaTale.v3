using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Execution;

public class ScenarioTestCollectionRunner :
    TestCollectionRunner<ScenarioTestCollectionRunnerContext, ScenarioTestCollection, ScenarioTestClass,
        ScenarioTestCase>
{
    public static ScenarioTestCollectionRunner Instance { get; } = new();

    protected override ValueTask<RunSummary> FailTestClass(
        ScenarioTestCollectionRunnerContext ctxt,
        ScenarioTestClass? testClass,
        IReadOnlyCollection<ScenarioTestCase> testCases,
        Exception exception)
    {
        var result = XunitRunnerHelper.FailTestCases(
            Guard.ArgumentNotNull(ctxt).MessageBus,
            ctxt.CancellationTokenSource,
            testCases,
            exception,
            sendTestClassMessages: true,
            sendTestMethodMessages: true
        );

        return new(result);
    }


    protected override async ValueTask<RunSummary> RunTestClass(
        ScenarioTestCollectionRunnerContext ctxt,
        ScenarioTestClass? testClass,
        IReadOnlyCollection<ScenarioTestCase> testCases)
    {
        ArgumentNullException.ThrowIfNull(testClass);

        object? testClassInstance = null;

        try
        {
            testClassInstance = Activator.CreateInstance(testClass.Class);
        }
        catch (Exception ex)
        {
            return await this.FailTestClass(ctxt, testClass, testCases, ex);
        }

        // if (testClassInstance is not Specification specification)
        // return await FailTestClass(ctxt, testClass, testCases, new TestPipelineException($"Test class {testClass.Class.FullName} cannot be static, and must derive from Specification."));

        try
        {
            // specification.OnStart();
        }
        catch (Exception ex)
        {
            return await this.FailTestClass(ctxt, testClass, testCases, ex);
        }

        var result = await ScenarioTestClassRunner.Instance.Run(testClass, testCases, ctxt.MessageBus,
            ctxt.Aggregator.Clone(), ctxt.CancellationTokenSource);

        // ctxt.Aggregator.Run(specification.OnFinish);

        // if (specification is IAsyncDisposable asyncDisposable)
        //     await asyncDisposable.DisposeAsync();
        // else if (specification is IDisposable disposable)
        //     disposable.Dispose();

        return result;
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
