using Xunit.Sdk;

namespace LambdaTale.v3.Tests.Harness;

internal sealed class SimpleDiscoveryOptions : ITestFrameworkDiscoveryOptions
{
    public TValue? GetValue<TValue>(string name) => default;
    public void SetValue<TValue>(string name, TValue value) { }
    public string ToJson() => "{}";
}
