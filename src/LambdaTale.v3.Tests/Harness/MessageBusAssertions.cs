using Xunit;
using Xunit.Sdk;

namespace LambdaTale.v3.Tests.Harness;

internal static class MessageBusAssertions
{
    public static ITestPassed AssertStepPassed(this CapturingMessageBus bus, string displayNameContains) =>
        bus.SingleOutcomeForStep<ITestPassed>(displayNameContains);

    public static ITestFailed AssertStepFailed(this CapturingMessageBus bus, string displayNameContains) =>
        bus.SingleOutcomeForStep<ITestFailed>(displayNameContains);

    public static ITestSkipped AssertStepSkipped(this CapturingMessageBus bus, string displayNameContains) =>
        bus.SingleOutcomeForStep<ITestSkipped>(displayNameContains);

    public static ITestStarting AssertStepStarted(this CapturingMessageBus bus, string displayNameContains) =>
        Assert.Single(bus.OfType<ITestStarting>(), m => m.TestDisplayName.Contains(displayNameContains));

    private static T SingleOutcomeForStep<T>(this CapturingMessageBus bus, string displayNameContains)
        where T : ITestResultMessage
    {
        var step = bus.AssertStepStarted(displayNameContains);
        return Assert.Single(bus.OfType<T>(), r => r.TestUniqueID == step.TestUniqueID);
    }
}
