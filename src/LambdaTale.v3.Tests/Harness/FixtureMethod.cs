using System.Reflection;
using Xunit.v3;

namespace LambdaTale.v3.Tests.Harness;

internal static class FixtureMethod
{
    public static XunitTestMethod For<TFixture>(string methodName)
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
        return new XunitTestMethod(testClass, method, testMethodArguments: []);
    }
}
