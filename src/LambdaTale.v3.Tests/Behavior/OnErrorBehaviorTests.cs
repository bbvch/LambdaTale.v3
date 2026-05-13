using LambdaTale.v3.Tests.Harness;
using Xunit;

namespace LambdaTale.v3.Tests.Behavior;

public class OnErrorBehaviorTests
{
    [Fact]
    public async Task ContinueOnErrorAllowsSubsequentStepsToRun()
    {
        var bus = await ScenarioTestRunner.RunFixture<ContinueOnErrorFixture>(
            nameof(ContinueOnErrorFixture.Scenario));

        var failed = bus.AssertStepFailed("Given a step that fails with ContinueOnError");
        Assert.Contains("intentional failure", failed.Messages.Single());

        bus.AssertStepPassed("Then the subsequent step still runs");
        bus.AssertStepPassed("And we can assert the subsequent step ran");
    }

    [Fact]
    public async Task StopOnErrorSkipsSubsequentSteps()
    {
        var bus = await ScenarioTestRunner.RunFixture<StopOnErrorFixture>(
            nameof(StopOnErrorFixture.Scenario));

        var failed = bus.AssertStepFailed("Given a StopOnError step that fails");
        Assert.Contains("intentional failure", failed.Messages.Single());

        var firstSkipped = bus.AssertStepSkipped("Then a ContinueOnError step is skipped");
        Assert.Equal("Previous step failed", firstSkipped.Reason);
        bus.AssertStepSkipped("And a second ContinueOnError step is also skipped");
        bus.AssertStepSkipped("And the variables are set");
    }

    private sealed class ContinueOnErrorFixture
    {
        public void Scenario()
        {
            var subsequentStepRan = false;
            "Given a step that fails with ContinueOnError".ContinueOnError(() =>
            {
                subsequentStepRan = false;
                throw new InvalidOperationException("intentional failure");
            });
            "Then the subsequent step still runs".x(() => subsequentStepRan = true);
            "And we can assert the subsequent step ran".x(() => Assert.True(subsequentStepRan));
        }
    }

    private sealed class StopOnErrorFixture
    {
        public void Scenario()
        {
            var step2Ran = false;
            var step3Ran = false;
            "Given a StopOnError step that fails".StopOnError(()
                => throw new InvalidOperationException("intentional failure"));
            "Then a ContinueOnError step is skipped".ContinueOnError(() => step2Ran = true);
            "And a second ContinueOnError step is also skipped".ContinueOnError(() => step3Ran = true);
            "And the variables are set".ContinueOnError(() =>
            {
                Assert.True(step2Ran);
                Assert.True(step3Ran);
            });
        }
    }
}
