using LambdaTale.v3.Tests.Harness;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Tests.Behavior;

public class SerializationBehaviorTests
{
    [Fact]
    public void RoundtripPreservesAllConfiguredFields()
    {
        var testMethod = FixtureMethod.For<Fixture>(nameof(Fixture.Method));
        var original = new ScenarioTestCase(
            testMethod: testMethod,
            testMethodArguments: ["hello", 42, null],
            testCaseDisplayName: "Custom Display",
            skipReason: "skip reason",
            sourceFilePath: "/path/to/file.cs",
            sourceLineNumber: 123,
            isDelayEnumerated: true,
            skipTestWithoutData: true,
            @explicit: true,
            skipExceptions: [typeof(InvalidOperationException), typeof(ArgumentException)],
            skipType: typeof(string),
            skipUnless: "PropX",
            skipWhen: null,
            timeout: 5000);

        var info = new InMemorySerializationInfo();
        original.Serialize(info);

#pragma warning disable CS0618 // [Obsolete] de-serialization ctor
        var restored = new ScenarioTestCase();
#pragma warning restore CS0618
        restored.Deserialize(info);

        Assert.Same(testMethod, restored.TestMethod);
        Assert.Equal("Custom Display", restored.TestCaseDisplayName);
        Assert.Equal("skip reason", restored.SkipReason);
        Assert.Equal("/path/to/file.cs", restored.SourceFilePath);
        Assert.Equal(123, restored.SourceLineNumber);
        Assert.True(((ITestCaseMetadata)restored).Explicit);
        Assert.True(((IXunitDelayEnumeratedTestCase)restored).SkipTestWithoutData);
        Assert.Equal(typeof(string), restored.SkipType);
        Assert.Equal("PropX", restored.SkipUnless);
        Assert.Null(restored.SkipWhen);
        Assert.Equal(5000, restored.Timeout);
        Assert.Equal([typeof(InvalidOperationException), typeof(ArgumentException)], restored.SkipExceptions);
        Assert.NotNull(restored.TestMethodArguments);
        Assert.Equal(3, restored.TestMethodArguments!.Length);
        Assert.Equal("hello", restored.TestMethodArguments[0]);
        Assert.Equal(42, restored.TestMethodArguments[1]);
        Assert.Null(restored.TestMethodArguments[2]);
    }

    private sealed class InMemorySerializationInfo : IXunitSerializationInfo
    {
        private readonly Dictionary<string, object?> values = [];

        public void AddValue(string key, object? value, Type? valueType) => this.values[key] = value;
        public object? GetValue(string key) => this.values.GetValueOrDefault(key);
    }

    private sealed class Fixture
    {
        public void Method() { }
    }
}
