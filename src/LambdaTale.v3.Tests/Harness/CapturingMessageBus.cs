using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Tests.Harness;

internal sealed class CapturingMessageBus : IMessageBus
{
    private readonly List<IMessageSinkMessage> messages = [];

    public IReadOnlyList<IMessageSinkMessage> Messages => this.messages;

    public IEnumerable<T> OfType<T>() => this.messages.OfType<T>();

    public bool QueueMessage(IMessageSinkMessage message)
    {
        this.messages.Add(message);
        return true;
    }

    public void Dispose() { }
}
