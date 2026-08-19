using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioStep(
    ScenarioTestCase parentTestCase,
    int stepIndex,
    string tale,
    object?[]? rowArgs,
    IReadOnlyList<string>? serializedRowArgs = null) : IXunitTest
{
    public ITestCase TestCase => parentTestCase;

    // Failures outside any step (construction, disposal, timeout) and a skipped case are reported
    // as pseudo-steps that carry a fixed name rather than a tale.
    internal static ScenarioStep Synthetic(ScenarioTestCase parentTestCase, int stepIndex, string displayName) =>
        new(parentTestCase, stepIndex, displayName, rowArgs: null) { DisplayNameOverride = displayName };

    internal string? DisplayNameOverride { get; private init; }

    public string? TestLabel => null;

    public string TestDisplayName
    {
        get
        {
            if (this.DisplayNameOverride is not null)
            {
                return this.DisplayNameOverride;
            }

            if (rowArgs is null || rowArgs.Length == 0)
            {
                return $"[{stepIndex}] {tale}";
            }

            var formatted = string.Join(", ", rowArgs.Select(FormatArg));
            return $"({formatted}) [{stepIndex}] {tale}";
        }
    }

    private static string FormatArg(object? arg) => arg switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => arg.ToString() ?? "null",
    };

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => parentTestCase.Traits;

    public string UniqueID
    {
        get
        {
            if (rowArgs is null || rowArgs.Length == 0)
            {
                return UniqueIDGenerator.ForTest(parentTestCase.UniqueID, stepIndex);
            }

            using var g = new UniqueIDGenerator();
            g.Add(parentTestCase.UniqueID);
            g.Add(stepIndex.ToString());
            if (serializedRowArgs is not null)
            {
                foreach (var serialized in serializedRowArgs)
                {
                    g.Add(serialized);
                }
            }
            else
            {
                foreach (var arg in rowArgs)
                {
                    g.Add(ScenarioTestCase.SerializeArgForId(arg));
                }
            }

            return g.Compute();
        }
    }

    IXunitTestCase IXunitTest.TestCase => parentTestCase;
    ICoreTestCase ICoreTest.TestCase => parentTestCase;
    string? IXunitTest.SkipReason => null;
    Type? IXunitTest.SkipType => null;
    string? IXunitTest.SkipUnless => null;
    string? IXunitTest.SkipWhen => null;
    IXunitTestMethod IXunitTest.TestMethod => parentTestCase.TestMethod;
    object?[] IXunitTest.TestMethodArguments => rowArgs ?? [];
    bool ICoreTest.Explicit => false;
    int ICoreTest.Timeout => 0;

    // Steps of a scenario are always run in order on a single flow; their parallelization
    // constraint is the one the containing test case carries.
    bool ICoreTest.DisableParallelization => parentTestCase.DisableParallelization;
}
