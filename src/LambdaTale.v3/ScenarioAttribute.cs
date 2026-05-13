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

    public string? DisplayName { get; init; }

    public bool Explicit { get; init; }

    public Type[]? SkipExceptions { get; init; }

    public Type? SkipType { get; init; }

    public string? SkipUnless { get; init; }

    public string? SkipWhen { get; init; }

    public int Timeout { get; init; }

    // IFactAttribute
    public string? Skip { get; init; }
    public string? SourceFilePath { get; } = sourceFilePath;
    public int? SourceLineNumber { get; } = sourceLineNumber < 1 ? null : sourceLineNumber;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreXunitAnalyzersRule1013Attribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class BackgroundAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class TeardownAttribute : Attribute { }
