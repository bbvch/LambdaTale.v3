using Xunit.Internal;
using Xunit.v3;

namespace LambdaTale.v3.Execution;

public class XunitAssemblyRunnerWrapper : XunitTestAssemblyRunner
{
    public static new XunitAssemblyRunnerWrapper Instance { get; } = new();

    protected override async ValueTask<bool> OnTestAssemblyFinished(
        XunitTestAssemblyRunnerContext ctxt,
        RunSummary summary)
    {
        _ = Guard.ArgumentNotNull(ctxt);

        await ctxt.Aggregator.RunAsync(ctxt.AssemblyFixtureMappings.DisposeAsync);
        if (ctxt.Aggregator.HasExceptions)
        {
            var exception = ctxt.Aggregator.ToException()!;
            ctxt.Aggregator.Clear();

            if (!await ctxt.Aggregator.RunAsync(() => this.OnTestAssemblyCleanupFailure(ctxt, exception), true))
            {
                ctxt.CancellationTokenSource.Cancel();
            }
        }

        // This is overridden to prevent the base class from sending the `TestAssemblyFinished` message.
        // That message will be sent by `LambdaTaleExecutorFacade` instead.
        return true;
    }
}
