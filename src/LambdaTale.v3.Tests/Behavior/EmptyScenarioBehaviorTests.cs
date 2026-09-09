using LambdaTale.v3.Tests.Harness;
using Xunit;
using Xunit.Sdk;

namespace LambdaTale.v3.Tests.Behavior;

// Regression coverage for #19: a [Scenario] registering no steps was reported as green.
public class EmptyScenarioBehaviorTests
{
    [Fact]
    public async Task ScenarioWithoutStepsReportsSyntheticFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<NoStepsFixture>(
            nameof(NoStepsFixture.ScenarioWithoutSteps));

        var failed = bus.AssertStepFailed("(No Steps)");
        Assert.Contains("no steps", failed.Messages.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScenarioWithoutStepsIsNeverReportedAsPassing()
    {
        var bus = await ScenarioTestRunner.RunFixture<NoStepsFixture>(
            nameof(NoStepsFixture.ScenarioWithoutSteps));

        Assert.Empty(bus.OfType<ITestPassed>());
    }

    [Fact]
    public async Task ScenarioWhoseStepsAreAllConditionedOutReportsSyntheticFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<NoStepsFixture>(
            nameof(NoStepsFixture.ScenarioWhoseStepsAreConditionedOut));

        bus.AssertStepFailed("(No Steps)");
        Assert.Empty(bus.OfType<ITestPassed>());
    }

    private sealed class NoStepsFixture
    {
        public void ScenarioWithoutSteps()
        {
        }

        public void ScenarioWhoseStepsAreConditionedOut()
        {
            if (bool.Parse(bool.FalseString))
            {
                "Given a step that is never registered".x(() => { });
            }
        }
    }
}
