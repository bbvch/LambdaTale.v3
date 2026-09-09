using LambdaTale.v3.Tests.Harness;
using Xunit;

namespace LambdaTale.v3.Tests.Behavior;

// Regression coverage for #17: on timeout nothing cancelled the scenario the runner gave up on,
// so the orphaned run kept reporting into an already-disposed context. A step body that is
// already running still runs to completion — what must stop is the reporting.
public class TimeoutBehaviorTests
{
    private const int TimeoutMilliseconds = 100;
    private const int StepDurationMilliseconds = 600;

    [Fact]
    public async Task TimedOutScenarioReportsSyntheticTimeoutFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<SlowScenarioFixture>(
            nameof(SlowScenarioFixture.ScenarioWithSlowFirstStep), timeout: TimeoutMilliseconds);

        var failed = bus.AssertStepFailed("(Timeout)");
        Assert.Contains("timeout", failed.Messages.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TimedOutScenarioEmitsNoResultsAfterTheRunnerReturns()
    {
        var bus = await ScenarioTestRunner.RunFixture<SlowScenarioFixture>(
            nameof(SlowScenarioFixture.ScenarioWithSlowFirstStep), timeout: TimeoutMilliseconds);

        var messagesAtReturn = bus.Messages.Count;

        // Outlive the orphaned scenario: uncancelled, it would report the two steps that follow.
        await Task.Delay(StepDurationMilliseconds * 3, TestContext.Current.CancellationToken);

        Assert.Equal(messagesAtReturn, bus.Messages.Count);
    }

    [Fact]
    public async Task TimedOutScenarioDoesNotStartStepsAfterTheTimeoutFires()
    {
        var bus = await ScenarioTestRunner.RunFixture<SlowScenarioFixture>(
            nameof(SlowScenarioFixture.ScenarioWithSlowFirstStep), timeout: TimeoutMilliseconds);

        await Task.Delay(StepDurationMilliseconds * 3, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            bus.OfType<Xunit.Sdk.ITestStarting>(),
            m => m.TestDisplayName.Contains("after the timeout"));
    }

    [Fact]
    public async Task TimedOutScenarioCancelsAStepThatHonoursTheToken()
    {
        CooperativeFixture.Reset();

        var bus = await ScenarioTestRunner.RunFixture<CooperativeFixture>(
            nameof(CooperativeFixture.ScenarioWithCooperativeStep), timeout: TimeoutMilliseconds);

        bus.AssertStepFailed("(Timeout)");

        var finished = await Task.WhenAny(
            CooperativeFixture.Cancelled.Task,
            Task.Delay(3000, TestContext.Current.CancellationToken));

        Assert.True(
            ReferenceEquals(finished, CooperativeFixture.Cancelled.Task),
            "step awaited TestContext.Current.CancellationToken but was never cancelled");
    }

    private sealed class SlowScenarioFixture
    {
        public void ScenarioWithSlowFirstStep()
        {
            "Given a step slower than the timeout".x(async () =>
                await Task.Delay(StepDurationMilliseconds));
            "Then a step that runs after the timeout".x(() => { });
            "And another step that runs after the timeout".x(() => { });
        }
    }

    private sealed class CooperativeFixture
    {
        public static TaskCompletionSource Cancelled = new();

        public static void Reset() => Cancelled = new TaskCompletionSource();

        public void ScenarioWithCooperativeStep() =>
            "Given a step that honours the cancellation token".x(async () =>
            {
                try
                {
                    await Task.Delay(10_000, TestContext.Current.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Cancelled.TrySetResult();
                    throw;
                }
            });
    }
}
