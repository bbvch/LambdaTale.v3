using System.Reflection;
using LambdaTale.v3.Tests.Harness;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

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

    [Fact]
    public async Task StepDisplayNameDoesNotRepeatRowArgumentsAlreadyInTheScenarioName()
    {
        var bus = await ScenarioTestRunner.RunFixture<DataDrivenFixture>(
            nameof(DataDrivenFixture.Scenario),
            testMethodArguments: [1, "one"]);

        var step = bus.AssertStepStarted("Given a step that passes");
        Assert.Equal("Scenario(value: 1, name: \"one\"): [0] Given a step that passes", step.TestDisplayName);
    }

    [Fact]
    public async Task StepDisplayNameCarriesRowArgumentsWhenTheScenarioNameCannotHoldThem()
    {
        var bus = await ScenarioTestRunner.RunFixture<DelayEnumeratedFixture>(
            nameof(DelayEnumeratedFixture.Scenario),
            isDelayEnumerated: true);

        var step = bus.AssertStepStarted("Given a step that passes");
        Assert.Equal("Scenario: (1, \"one\") [0] Given a step that passes", step.TestDisplayName);
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class SingleRowDelayEnumeratedDataAttribute : DataAttribute
    {
        public override bool SupportsDiscoveryEnumeration() => false;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
            MethodInfo testMethod, DisposalTracker disposalTracker) =>
            new([new TheoryDataRow<int, string>(1, "one")]);
    }

    private sealed class SingleStepFixture
    {
        public void Scenario() =>
            "Given a step that passes".x(() => { });
    }

    private sealed class DataDrivenFixture
    {
        public void Scenario(int value, string name) =>
            "Given a step that passes".x(() => { });
    }

    private sealed class DelayEnumeratedFixture
    {
        [SingleRowDelayEnumeratedData]
        public void Scenario(int value, string name) =>
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
