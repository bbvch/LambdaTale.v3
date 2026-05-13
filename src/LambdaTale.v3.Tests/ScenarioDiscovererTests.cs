using System.Reflection;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Tests;

public class ScenarioDiscovererTests
{
    [AttributeUsage(AttributeTargets.Method)]
    private sealed class ThrowingDataAttribute : DataAttribute
    {
        public static readonly Exception ThrownException =
            new InvalidOperationException("data attribute exploded");

        public override bool SupportsDiscoveryEnumeration() => true;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
            MethodInfo testMethod, DisposalTracker disposalTracker) =>
            ValueTask.FromException<IReadOnlyCollection<ITheoryDataRow>>(ThrownException);
    }

    private sealed class SimpleDiscoveryOptions : ITestFrameworkDiscoveryOptions
    {
        public TValue? GetValue<TValue>(string name) => default;
        public void SetValue<TValue>(string name, TValue value) { }
        public string ToJson() => "{}";
    }

    [Fact]
    [ThrowingDataAttribute]
    public async Task Discover_WhenDataAttributeThrows_ReturnsExecutionErrorTestCase()
    {
        // Arrange — real IXunitTestMethod from the current test; no mocking needed.
        // DataAttributes includes ThrowingDataAttribute because it annotates this method.
        var testMethod = (IXunitTestMethod)TestContext.Current.TestMethod!;
        var discoverer = new ScenarioDiscoverer();
        var discoveryOptions = new SimpleDiscoveryOptions();
        var factAttribute = new ScenarioAttribute();

        // Pre-compute the expected unique ID using the same derivation as the implementation
        var g = new UniqueIDGenerator();
        g.Add(testMethod.UniqueID);
        g.Add("discovery-error");
        var expectedUniqueId = g.Compute();

        // Act
        var result = await discoverer.Discover(discoveryOptions, testMethod, factAttribute);

        // Assert
        var single = Assert.Single(result);
        var errorCase = Assert.IsType<ExecutionErrorTestCase>(single);
        Assert.Equal(testMethod.MethodName, errorCase.TestCaseDisplayName);
        Assert.Equal(expectedUniqueId, errorCase.UniqueID);
        Assert.Equal(ThrowingDataAttribute.ThrownException.Message, errorCase.ErrorMessage);
    }
}
