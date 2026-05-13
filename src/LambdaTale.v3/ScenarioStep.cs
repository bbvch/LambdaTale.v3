using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioStep : IXunitTest
{
    public ScenarioStep(
        ScenarioTestCase parentTestCase,
        int stepIndex,
        string tale,
        object?[]? rowArgs)
    {
        this.ParentTestCase = parentTestCase;
        this.StepIndex = stepIndex;
        this.Tale = tale;
        this.rowArgs = rowArgs;
    }

    private readonly ScenarioTestCase ParentTestCase;
    private readonly int StepIndex;
    private readonly string Tale;
    private readonly object?[]? rowArgs;

    public ITestCase TestCase => this.ParentTestCase;

    public string TestDisplayName
    {
        get
        {
            if (this.rowArgs is null || this.rowArgs.Length == 0)
            {
                return $"[{this.StepIndex}] {this.Tale}";
            }

            var formatted = string.Join(", ", this.rowArgs.Select(FormatArg));
            return $"({formatted}) [{this.StepIndex}] {this.Tale}";
        }
    }

    private static string FormatArg(object? arg) => arg switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => arg.ToString() ?? "null",
    };

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.ParentTestCase.Traits;

    public string UniqueID
    {
        get
        {
            if (this.rowArgs is null || this.rowArgs.Length == 0)
            {
                return UniqueIDGenerator.ForTest(this.ParentTestCase.UniqueID, this.StepIndex);
            }

            using var g = new UniqueIDGenerator();
            g.Add(this.ParentTestCase.UniqueID);
            g.Add(this.StepIndex.ToString());
            foreach (var arg in this.rowArgs)
            {
                g.Add(SerializationHelper.Instance.Serialize(arg));
            }

            return g.Compute();
        }
    }

    IXunitTestCase IXunitTest.TestCase => this.ParentTestCase;
    bool IXunitTest.Explicit => false;
    string? IXunitTest.SkipReason => null;
    Type? IXunitTest.SkipType => null;
    string? IXunitTest.SkipUnless => null;
    string? IXunitTest.SkipWhen => null;
    IXunitTestMethod IXunitTest.TestMethod => this.ParentTestCase.TestMethod;
    object?[] IXunitTest.TestMethodArguments => this.rowArgs ?? [];
    int IXunitTest.Timeout => 0;
}
