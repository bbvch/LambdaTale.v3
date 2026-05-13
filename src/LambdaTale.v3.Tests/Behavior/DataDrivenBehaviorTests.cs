using System.Reflection;
using LambdaTale.v3.Tests.Harness;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Tests.Behavior;

public class DataDrivenBehaviorTests
{
    [Fact]
    public async Task DelayEnumeratedDataIsResolvedAtExecutionTime()
    {
        var bus = await ScenarioTestRunner.RunFixture<DelayEnumeratedFixture>(
            nameof(DelayEnumeratedFixture.Scenario),
            isDelayEnumerated: true);

        bus.AssertStepPassed("value is 1");
        bus.AssertStepPassed("value is 2");
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class TwoRowsDataAttribute : DataAttribute
    {
        public override bool SupportsDiscoveryEnumeration() => false;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
            MethodInfo testMethod, DisposalTracker disposalTracker) =>
            new([new TheoryDataRow<int>(1), new TheoryDataRow<int>(2)]);
    }

    private sealed class DelayEnumeratedFixture
    {
        [TwoRowsData]
        public void Scenario(int value) =>
            $"value is {value}".x(() => Assert.True(value > 0));
    }
}
