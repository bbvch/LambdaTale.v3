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

    public string TestDisplayName
    {
        get
        {
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
    bool IXunitTest.Explicit => false;
    string? IXunitTest.SkipReason => null;
    Type? IXunitTest.SkipType => null;
    string? IXunitTest.SkipUnless => null;
    string? IXunitTest.SkipWhen => null;
    IXunitTestMethod IXunitTest.TestMethod => parentTestCase.TestMethod;
    object?[] IXunitTest.TestMethodArguments => rowArgs ?? [];
    int IXunitTest.Timeout => 0;
}
