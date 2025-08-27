using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Execution;

public class ScenarioStepRunner : TestRunner<ScenarioStepRunnerContext, ScenarioStep>
{
    public static ScenarioStepRunner Instance { get; } = new();

    // We don't want to create a new TestClass here
    protected override
        ValueTask<(object? Instance, SynchronizationContext? SyncContext, ExecutionContext? ExecutionContext)>
        CreateTestClassInstance(ScenarioStepRunnerContext ctxt) =>
        throw new NotSupportedException();

    protected override bool IsTestClassCreatable(ScenarioStepRunnerContext ctxt) => false;

    protected override bool IsTestClassDisposable(ScenarioStepRunnerContext ctxt, object testClassInstance) => false;

    protected override ValueTask<TimeSpan> InvokeTest(ScenarioStepRunnerContext ctxt, object? testClassInstance)
    {
        var instance = Activator.CreateInstance(ctxt.Test.Lambda.Method.DeclaringType); // TODO do this correctly (probably override execution to handle lambdas)
        return base.InvokeTest(ctxt, instance);
    }

    public async ValueTask<RunSummary> Run(
        ScenarioStep step,
        IMessageBus messageBus,
        string? skipReason,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        await using var ctxt =
            new ScenarioStepRunnerContext(step, messageBus, skipReason, aggregator, cancellationTokenSource);
        await ctxt.InitializeAsync();

        return await this.Run(ctxt);
    }
}

public class ScenarioStepRunnerContext(
    ScenarioStep test,
    IMessageBus messageBus,
    string? skipReason,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource) :
    TestRunnerContext<ScenarioStep>(test, messageBus, skipReason, ExplicitOption.Off, aggregator,
        cancellationTokenSource, test.Lambda.Method, []);
