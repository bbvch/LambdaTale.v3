using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using LambdaTale.v3.Tests.Harness;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Tests.Behavior;

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

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class EmptyDataAttribute : DataAttribute
    {
        public override bool SupportsDiscoveryEnumeration() => true;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
            MethodInfo testMethod, DisposalTracker disposalTracker) =>
            new([]);
    }

    public sealed class Unserializable(string name)
    {
        public override string ToString() => name;
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class NonSerializableDataAttribute : DataAttribute
    {
        public override bool SupportsDiscoveryEnumeration() => true;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
            MethodInfo testMethod, DisposalTracker disposalTracker) =>
            new([new TheoryDataRow<Unserializable>(new Unserializable("nope"))]);
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class ValuelessRowDataAttribute : DataAttribute
    {
        public override bool SupportsDiscoveryEnumeration() => true;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
            MethodInfo testMethod, DisposalTracker disposalTracker) =>
            new([new TheoryDataRow()]);
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class DelayEnumeratedDataAttribute : DataAttribute
    {
        public override bool SupportsDiscoveryEnumeration() => false;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
            MethodInfo testMethod, DisposalTracker disposalTracker) =>
            new([new TheoryDataRow<int>(1)]);
    }

    [Fact]
    [ThrowingData]
    [SuppressMessage(
        "Usage",
        "xUnit1005:Fact methods should not have test data",
        Justification = "ThrowingDataAttribute is attached deliberately so that testMethod.DataAttributes surfaces it to the discoverer under test.")]
    public async Task DiscoverReturnsExecutionErrorTestCaseWhenDataAttributeThrows()
    {
        var testMethod = (IXunitTestMethod)TestContext.Current.TestMethod!;
        var discoverer = new ScenarioDiscoverer();

        using var g = new UniqueIDGenerator();
        g.Add(testMethod.UniqueID);
        g.Add("discovery-error");
        var expectedUniqueId = g.Compute();

        var result = await discoverer.Discover(new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute());

        var single = Assert.Single(result);
        var errorCase = Assert.IsType<ExecutionErrorTestCase>(single);
        Assert.Equal(testMethod.MethodName, errorCase.TestCaseDisplayName);
        Assert.Equal(expectedUniqueId, errorCase.UniqueID);
        Assert.Equal(ThrowingDataAttribute.ThrownException.Message, errorCase.ErrorMessage);
    }

    [Fact]
    public async Task DiscoverReturnsSingleTestCaseWhenNoDataAttributes()
    {
        var testMethod = FixtureMethod.For<NoDataFixture>(nameof(NoDataFixture.Method));
        var result = await new ScenarioDiscoverer().Discover(
            new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute());

        var single = Assert.Single(result);
        Assert.IsType<ScenarioTestCase>(single);
    }

    [Fact]
    public async Task DiscoverReturnsEmptyWhenNoDataAttributesAndSkipTestWithoutData()
    {
        var testMethod = FixtureMethod.For<NoDataFixture>(nameof(NoDataFixture.Method));
        var result = await new ScenarioDiscoverer().Discover(
            new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute { SkipTestWithoutData = true });

        Assert.Empty(result);
    }

    [Fact]
    public async Task DiscoverReturnsTestCasePerRowWhenDataEnumeratesAtDiscoveryTime()
    {
        var testMethod = FixtureMethod.For<WithInlineDataFixture>(nameof(WithInlineDataFixture.Method));
        var result = await new ScenarioDiscoverer().Discover(
            new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute());

        Assert.Equal(3, result.Count);
        Assert.All(result, tc => Assert.IsType<ScenarioTestCase>(tc));
    }

    [Fact]
    public async Task DiscoverAppliesAttributeSkipReasonToAllDataRows()
    {
        var testMethod = FixtureMethod.For<WithInlineDataFixture>(nameof(WithInlineDataFixture.Method));
        var result = await new ScenarioDiscoverer().Discover(
            new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute { Skip = "demonstrating skip with data" });

        Assert.Equal(3, result.Count);
        Assert.All(result, tc => Assert.Equal("demonstrating skip with data", Assert.IsType<ScenarioTestCase>(tc).SkipReason));
    }

    [Fact]
    public async Task DiscoverReturnsSkippedTestCaseWhenSkipTestWithoutDataAndDataIsEmpty()
    {
        var testMethod = FixtureMethod.For<EmptyDataFixture>(nameof(EmptyDataFixture.Method));
        var result = await new ScenarioDiscoverer().Discover(
            new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute { SkipTestWithoutData = true });

        var single = Assert.Single(result);
        var testCase = Assert.IsType<ScenarioTestCase>(single);
        Assert.Equal("No data found for scenario", testCase.SkipReason);
    }

    [Fact]
    public async Task DiscoverReturnsTestCaseWhenRowArgumentIsNotXunitSerializable()
    {
        var testMethod = FixtureMethod.For<NonSerializableDataFixture>(nameof(NonSerializableDataFixture.Method));
        var result = await new ScenarioDiscoverer().Discover(
            new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute());

        var single = Assert.Single(result);
        var testCase = Assert.IsType<ScenarioTestCase>(single);
        Assert.NotEmpty(testCase.UniqueID);
    }

    [Fact]
    public async Task DiscoverOmitsParametersTheDataRowDoesNotSupplyFromTheDisplayName()
    {
        var testMethod = FixtureMethod.For<VariableParametersFixture>(nameof(VariableParametersFixture.Method));
        var result = await new ScenarioDiscoverer().Discover(
            new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute());

        var testCase = Assert.IsType<ScenarioTestCase>(Assert.Single(result));
        Assert.DoesNotContain("???", testCase.TestCaseDisplayName, StringComparison.Ordinal);
        Assert.EndsWith("Method(value: 5)", testCase.TestCaseDisplayName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DiscoverOmitsTheArgumentListEntirelyWhenEveryParameterIsAVariable()
    {
        var testMethod = FixtureMethod.For<AllVariableParametersFixture>(nameof(AllVariableParametersFixture.Method));
        var result = await new ScenarioDiscoverer().Discover(
            new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute());

        var testCase = Assert.IsType<ScenarioTestCase>(Assert.Single(result));
        Assert.EndsWith("Method", testCase.TestCaseDisplayName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DiscoverReturnsDelayEnumeratedCaseWhenDataDoesNotSupportEnumeration()
    {
        var testMethod = FixtureMethod.For<DelayEnumeratedFixture>(nameof(DelayEnumeratedFixture.Method));
        var result = await new ScenarioDiscoverer().Discover(
            new SimpleDiscoveryOptions(), testMethod, new ScenarioAttribute());

        var single = Assert.Single(result);
        Assert.IsAssignableFrom<IXunitDelayEnumeratedTestCase>(single);
    }

    private sealed class NoDataFixture
    {
        public void Method() { }
    }

    private sealed class WithInlineDataFixture
    {
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Method(int value) { }
    }

    private sealed class EmptyDataFixture
    {
        [EmptyData]
        public void Method() { }
    }

    private sealed class NonSerializableDataFixture
    {
        [NonSerializableData]
        public void Method(Unserializable value) { }
    }

    private sealed class DelayEnumeratedFixture
    {
        [DelayEnumeratedData]
        public void Method(int value) { }
    }

    private sealed class VariableParametersFixture
    {
        [InlineData(5)]
        public void Method(int value, string varAlpha, string varBeta) { }
    }

    private sealed class AllVariableParametersFixture
    {
        [ValuelessRowData]
        public void Method(string varAlpha, string varBeta) { }
    }
}
