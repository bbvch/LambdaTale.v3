using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Tests.Harness;

internal sealed class CapturingMessageBus : IMessageBus
{
    private readonly Lock gate = new();
    private readonly List<IMessageSinkMessage> messages = [];

    public IReadOnlyList<IMessageSinkMessage> Messages
    {
        get
        {
            lock (this.gate)
            {
                return [.. this.messages];
            }
        }
    }

    public IEnumerable<T> OfType<T>() => this.Messages.OfType<T>();

    public bool QueueMessage(IMessageSinkMessage message)
    {
        lock (this.gate)
        {
            this.messages.Add(message);
        }

        return true;
    }

    public void Dispose() { }
}
