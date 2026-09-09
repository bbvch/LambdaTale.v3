using LambdaTale.v3.Tests.Harness;
using Xunit;
using Xunit.Sdk;

namespace LambdaTale.v3.Tests.Behavior;

// Regression coverage for #15: an exception outside any step was reported as a cleanup failure,
// and the steps the scenario had already registered never reached the summary.
public class ScenarioBodyFailureBehaviorTests
{
    [Fact]
    public async Task ExceptionOutsideAStepIsReportedAsSyntheticFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<ThrowsOutsideStepFixture>(
            nameof(ThrowsOutsideStepFixture.ScenarioThrowsAfterRegisteringSteps));

        var failed = bus.AssertStepFailed("(Scenario)");
        Assert.Contains("thrown outside of a step", failed.Messages.Single());
    }

    [Fact]
    public async Task ExceptionOutsideAStepIsNotReportedAsCleanupFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<ThrowsOutsideStepFixture>(
            nameof(ThrowsOutsideStepFixture.ScenarioThrowsAfterRegisteringSteps));

        Assert.Empty(bus.OfType<ITestCaseCleanupFailure>());
    }

    [Fact]
    public async Task StepsRegisteredBeforeTheThrowStillAppearInTheSummary()
    {
        var bus = await ScenarioTestRunner.RunFixture<ThrowsOutsideStepFixture>(
            nameof(ThrowsOutsideStepFixture.ScenarioThrowsAfterRegisteringSteps));

        bus.AssertStepSkipped("Given a step registered before the throw");
        bus.AssertStepSkipped("Then a second step registered before the throw");
    }

    [Fact]
    public async Task AsyncExceptionOutsideAStepIsReportedAsSyntheticFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<ThrowsOutsideStepFixture>(
            nameof(ThrowsOutsideStepFixture.AsyncScenarioThrowsAfterRegisteringSteps));

        var failed = bus.AssertStepFailed("(Scenario)");
        Assert.Contains("thrown outside of a step", failed.Messages.Single());
        Assert.Empty(bus.OfType<ITestCaseCleanupFailure>());
    }

    private sealed class ThrowsOutsideStepFixture
    {
        public void ScenarioThrowsAfterRegisteringSteps()
        {
            "Given a step registered before the throw".x(() => { });
            "Then a second step registered before the throw".x(() => { });

            throw new InvalidOperationException("thrown outside of a step");
        }

        public async Task AsyncScenarioThrowsAfterRegisteringSteps()
        {
            "Given a step registered before the async throw".x(() => { });
            await Task.Yield();

            throw new InvalidOperationException("thrown outside of a step");
        }
    }
}
