using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

namespace LambdaTale.v3;

[AttributeUsage(AttributeTargets.Method)]
[XunitTestCaseDiscoverer(typeof(ScenarioDiscoverer))]
public sealed class ScenarioAttribute(
    [CallerFilePath] string sourceFilePath = "",
    [CallerLineNumber] int sourceLineNumber = -1)
    : FactAttribute(sourceFilePath, sourceLineNumber)
{
    public bool SkipTestWithoutData { get; init; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class BackgroundAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TeardownAttribute : Attribute;
