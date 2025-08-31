using System.Runtime.CompilerServices;

namespace LambdaTale.v3;

[AttributeUsage(AttributeTargets.Method)]
[IgnoreXunitAnalyzersRule1013]
public class ScenarioAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1) : Attribute
{
    public bool DisableDiscoveryEnumeration { get; }

    public bool SkipTestWithoutData { get; }

    public string? SourceFilePath { get; } = sourceFilePath;

    public int? SourceLineNumber { get; } = sourceLineNumber < 1 ? null : sourceLineNumber;
}

public sealed class IgnoreXunitAnalyzersRule1013Attribute : Attribute
{
}
