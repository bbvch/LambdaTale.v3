using LambdaTale.v3.Tests.Harness;
using Xunit;

namespace LambdaTale.v3.Tests.Behavior;

public class CancellationTokenStepBehaviorTests
{
    private const int TimeoutMilliseconds = 100;

    [Fact]
    public async Task StepReceivesTheTokenOfItsTestContext()
    {
        var bus = await ScenarioTestRunner.RunFixture<TokenFixture>(nameof(TokenFixture.ScenarioComparingTokens));

        bus.AssertStepPassed("Given a step handed a token");
    }

    [Fact]
    public async Task TimedOutScenarioCancelsTheTokenHandedToTheStep()
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
            "step awaited the token it was handed but was never cancelled");
    }

    [Fact]
    public async Task ContinueOnErrorWithTokenAllowsSubsequentStepsToRun()
    {
        var bus = await ScenarioTestRunner.RunFixture<OnErrorFixture>(nameof(OnErrorFixture.ContinueScenario));

        var failed = bus.AssertStepFailed("Given a failing ContinueOnError step");
        Assert.Contains("intentional failure", failed.Messages.Single());
        bus.AssertStepPassed("Then the subsequent step still runs");
    }

    [Fact]
    public async Task StopOnErrorWithTokenSkipsSubsequentSteps()
    {
        var bus = await ScenarioTestRunner.RunFixture<OnErrorFixture>(nameof(OnErrorFixture.StopScenario));

        var failed = bus.AssertStepFailed("Given a failing StopOnError step");
        Assert.Contains("intentional failure", failed.Messages.Single());
        var skipped = bus.AssertStepSkipped("Then the subsequent step is skipped");
        Assert.Equal("Previous step failed", skipped.Reason);
    }

    [Fact]
    public async Task XWithTokenAndContinueOptionAllowsSubsequentStepsToRun()
    {
        var bus = await ScenarioTestRunner.RunFixture<OnErrorFixture>(nameof(OnErrorFixture.ExplicitContinueScenario));

        bus.AssertStepFailed("Given a failing step with OnError.Continue");
        bus.AssertStepPassed("Then the subsequent step still runs");
    }

    private sealed class TokenFixture
    {
        public void ScenarioComparingTokens() =>
            "Given a step handed a token".x(cancellationToken =>
            {
                Assert.Equal(TestContext.Current.CancellationToken, cancellationToken);
                Assert.True(cancellationToken.CanBeCanceled);
                return Task.CompletedTask;
            });
    }

    private sealed class CooperativeFixture
    {
        public static TaskCompletionSource Cancelled = new();

        public static void Reset() => Cancelled = new TaskCompletionSource();

        public void ScenarioWithCooperativeStep() =>
            "Given a step that honours the token it is handed".x(async cancellationToken =>
            {
                try
                {
                    await Task.Delay(10_000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Cancelled.TrySetResult();
                    throw;
                }
            });
    }

    private sealed class OnErrorFixture
    {
        public void ContinueScenario()
        {
            "Given a failing ContinueOnError step".ContinueOnError(_ =>
                throw new InvalidOperationException("intentional failure"));
            "Then the subsequent step still runs".x(_ => Task.CompletedTask);
        }

        public void StopScenario()
        {
            "Given a failing StopOnError step".StopOnError(_ =>
                throw new InvalidOperationException("intentional failure"));
            "Then the subsequent step is skipped".ContinueOnError(_ => Task.CompletedTask);
        }

        public void ExplicitContinueScenario()
        {
            "Given a failing step with OnError.Continue".x(
                _ => throw new InvalidOperationException("intentional failure"),
                OnError.Continue);
            "Then the subsequent step still runs".x(_ => Task.CompletedTask);
        }
    }
}
