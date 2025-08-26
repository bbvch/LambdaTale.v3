using System.Runtime.CompilerServices;

namespace LambdaTale.v3;

[AttributeUsage(AttributeTargets.Method)]
public class ScenarioAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1) : Attribute
{
    public bool DisableDiscoveryEnumeration { get; }

    public bool SkipTestWithoutData { get; }
}
