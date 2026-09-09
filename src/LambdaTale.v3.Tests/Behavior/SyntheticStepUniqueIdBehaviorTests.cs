using LambdaTale.v3.Tests.Harness;
using Xunit;
using Xunit.Sdk;

namespace LambdaTale.v3.Tests.Behavior;

// Regression coverage for #16: synthetic steps drew their index from the same zero-based counter
// as real steps, so (Dispose) and step [0] shared a unique ID.
public class SyntheticStepUniqueIdBehaviorTests
{
    [Fact]
    public async Task SyntheticStepDoesNotShareUniqueIdWithFirstRealStep()
    {
        var bus = await ScenarioTestRunner.RunFixture<StepThenFailingDisposeFixture>(
            nameof(StepThenFailingDisposeFixture.ScenarioWithOneStep));

        var realStep = bus.AssertStepStarted("[0] Given a step that passes");
        var synthetic = bus.AssertStepStarted("(Dispose)");

        Assert.NotEqual(realStep.TestUniqueID, synthetic.TestUniqueID);
    }

    [Fact]
    public async Task EveryStepInACaseGetsADistinctUniqueId()
    {
        var bus = await ScenarioTestRunner.RunFixture<StepThenFailingDisposeFixture>(
            nameof(StepThenFailingDisposeFixture.ScenarioWithSeveralSteps));

        var ids = bus.OfType<ITestStarting>().Select(m => m.TestUniqueID).ToList();

        Assert.NotEmpty(ids);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    private sealed class StepThenFailingDisposeFixture : IDisposable
    {
        public void ScenarioWithOneStep() =>
            "Given a step that passes".x(() => { });

        public void ScenarioWithSeveralSteps()
        {
            "Given a first step".x(() => { });
            "Then a second step".x(() => { });
            "And a third step".x(() => { });
        }

        public void Dispose() =>
            throw new InvalidOperationException("dispose fails to force a synthetic step");
    }
}
