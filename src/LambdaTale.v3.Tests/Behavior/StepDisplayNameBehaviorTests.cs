using LambdaTale.v3.Tests.Harness;
using Xunit;

namespace LambdaTale.v3.Tests.Behavior;

public class StepDisplayNameBehaviorTests
{
    [Fact]
    public async Task StepDisplayNameIsQualifiedWithScenarioName()
    {
        var bus = await ScenarioTestRunner.RunFixture<SingleStepFixture>(
            nameof(SingleStepFixture.Scenario),
            testCaseDisplayName: "My Named Scenario");

        bus.AssertStepPassed("My Named Scenario: [0] Given a step that passes");
    }

    [Fact]
    public async Task SyntheticFailureDisplayNameIsQualifiedWithScenarioName()
    {
        var bus = await ScenarioTestRunner.RunFixture<ConstructorThrowsFixture>(
            nameof(ConstructorThrowsFixture.ScenarioStepsDoNotRun),
            testCaseDisplayName: "My Named Scenario");

        bus.AssertStepFailed("My Named Scenario: (Constructor)");
    }

    private sealed class SingleStepFixture
    {
        public void Scenario() =>
            "Given a step that passes".x(() => { });
    }

    private sealed class ConstructorThrowsFixture
    {
        public ConstructorThrowsFixture() =>
            throw new InvalidOperationException("constructor failed");

        public void ScenarioStepsDoNotRun() =>
            "Then this step should not execute".x(()
                => throw new InvalidOperationException("should not run"));
    }
}
