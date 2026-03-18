using System.Runtime.CompilerServices;
using Xunit.v3;

namespace LambdaTale.v3;

[AttributeUsage(AttributeTargets.Method)]
[XunitTestCaseDiscoverer(typeof(ScenarioDiscoverer))]
[IgnoreXunitAnalyzersRule1013]
public sealed class ScenarioAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1) : Attribute, IFactAttribute
{
    public bool SkipTestWithoutData { get; init; }

    // IFactAttribute
    string? IFactAttribute.DisplayName => null;
    bool IFactAttribute.Explicit => false;
    public string? Skip { get; init; }
    Type[]? IFactAttribute.SkipExceptions => null;
    Type? IFactAttribute.SkipType => null;
    string? IFactAttribute.SkipUnless => null;
    string? IFactAttribute.SkipWhen => null;
    public string? SourceFilePath { get; } = sourceFilePath;
    public int? SourceLineNumber { get; } = sourceLineNumber < 1 ? null : sourceLineNumber;
    int IFactAttribute.Timeout => 0;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreXunitAnalyzersRule1013Attribute : Attribute
{
}
