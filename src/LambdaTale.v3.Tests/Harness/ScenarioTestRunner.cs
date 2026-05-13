using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Tests.Harness;

// Fixtures passed in as TFixture should be `private sealed` nested classes so xUnit's
// own discovery skips them. The fixture method does NOT need [Scenario] — Run() invokes
// the MethodInfo directly. [Background]/[Teardown] DO matter since RunWithArguments
// reflects for them.
internal static class ScenarioTestRunner
{
    public static async Task<CapturingMessageBus> RunFixture<TFixture>(string methodName)
    {
        var bus = new CapturingMessageBus();
        var testCase = BuildTestCase<TFixture>(methodName);
        await testCase.Run(
            ExplicitOption.Off,
            bus,
            constructorArguments: [],
            new ExceptionAggregator(),
            new CancellationTokenSource());
        return bus;
    }

    private static ScenarioTestCase BuildTestCase<TFixture>(string methodName)
    {
        var type = typeof(TFixture);
        var method = type.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new ArgumentException($"Method '{methodName}' not found on {type.FullName}", nameof(methodName));

        var assembly = new XunitTestAssembly(type.Assembly);
        var collection = new XunitTestCollection(
            assembly,
            collectionDefinition: null,
            disableParallelization: true,
            displayName: $"Fixture: {type.Name}");
        var testClass = new XunitTestClass(type, collection);
        var testMethod = new XunitTestMethod(testClass, method, testMethodArguments: []);

        return new ScenarioTestCase(testMethod);
    }
}
