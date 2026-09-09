using LambdaTale.v3.Tests.Harness;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Tests.Behavior;

public class RunCancellationBehaviorTests
{
    [Fact]
    public async Task RefusedMessageCancelsTheRunnersOwnTokenSource()
    {
        var cancellation = new CancellationTokenSource();

        await RunFixture(new RefusingBus(), cancellation);

        Assert.True(cancellation.IsCancellationRequested);
    }

    [Fact]
    public async Task AcceptedMessagesLeaveTheRunnersTokenSourceAlone()
    {
        var cancellation = new CancellationTokenSource();

        await RunFixture(new CapturingMessageBus(), cancellation);

        Assert.False(cancellation.IsCancellationRequested);
    }

    [Fact]
    public async Task TimeoutDoesNotCancelTheRunnersTokenSource()
    {
        var cancellation = new CancellationTokenSource();

        await RunFixture(new CapturingMessageBus(), cancellation, timeout: 100, slow: true);

        Assert.False(cancellation.IsCancellationRequested);
    }

    private static async Task RunFixture(
        IMessageBus bus,
        CancellationTokenSource cancellation,
        int timeout = 0,
        bool slow = false)
    {
        var methodName = slow ? nameof(Fixture.SlowScenario) : nameof(Fixture.Scenario);
        var testMethod = FixtureMethod.For<Fixture>(methodName);
        var testCase = new ScenarioTestCase(testMethod, testMethodArguments: null, timeout: timeout);

        await using var scheduler = ExecutionScheduler.CreateUnlimited();
        await using var methodFixtures = new FixtureMappingManager("Method");

        await testCase.Run(
            ExplicitOption.Off,
            bus,
            constructorArguments: [],
            new ExceptionAggregator(),
            cancellation,
            ParallelMode.None,
            scheduler,
            methodFixtures);
    }

    private sealed class RefusingBus : IMessageBus
    {
        private bool refused;

        public bool QueueMessage(IMessageSinkMessage message)
        {
            if (message is not ITestFinished || this.refused)
            {
                return true;
            }

            this.refused = true;
            return false;
        }

        public void Dispose() { }
    }

    private sealed class Fixture
    {
        public void Scenario()
        {
            "Given a first step".x(() => { });
            "Then a second step".x(() => { });
        }

        public void SlowScenario() =>
            "Given a step slower than the timeout".x(async () => await Task.Delay(600));
    }
}
