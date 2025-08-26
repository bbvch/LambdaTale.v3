using System.Runtime.CompilerServices;
using Xunit;

namespace LambdaTale.v3;

[AttributeUsage(AttributeTargets.Method)]
public class ScenarioAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1) :
    FactAttribute(sourceFilePath, sourceLineNumber)
{
    public bool DisableDiscoveryEnumeration { get; }

    public bool SkipTestWithoutData { get; }
}
