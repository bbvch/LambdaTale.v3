using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

// Lets a case stop reporting at a point of its choosing, so a timed-out scenario's remaining
// work does not queue results against a context the runner has already disposed.
internal sealed class StoppableMessageBus(IMessageBus inner) : IMessageBus
{
    private volatile bool stopped;

    public void Stop() => this.stopped = true;

    // True rather than false when stopped: false would tell the runner to abandon the whole run.
    public bool QueueMessage(IMessageSinkMessage message) =>
        this.stopped || inner.QueueMessage(message);

    public void Dispose() { }
}
