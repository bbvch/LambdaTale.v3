using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioStep : IXunitTest
{
    public ScenarioStep(
        ScenarioTestCase parentTestCase,
        int stepIndex,
        string tale)
    {
        this.ParentTestCase = parentTestCase;
        this.StepIndex = stepIndex;
        this.Tale = tale;
    }

    private readonly ScenarioTestCase ParentTestCase;
    private readonly int StepIndex;
    private readonly string Tale;

    public ITestCase TestCase => this.ParentTestCase;
    public string TestDisplayName
    {
        get
        {
            var args = this.ParentTestCase.TestMethodArguments;
            if (args is null || args.Length == 0)
            {
                return $"[{this.StepIndex}] {this.Tale}";
            }

            var formatted = string.Join(", ", args.Select(FormatArg));
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
    public string UniqueID => UniqueIDGenerator.ForTest(this.ParentTestCase.UniqueID, this.StepIndex);

    IXunitTestCase IXunitTest.TestCase => this.ParentTestCase;
    bool IXunitTest.Explicit => false;
    string? IXunitTest.SkipReason => null;
    Type? IXunitTest.SkipType => null;
    string? IXunitTest.SkipUnless => null;
    string? IXunitTest.SkipWhen => null;
    IXunitTestMethod IXunitTest.TestMethod => this.ParentTestCase.TestMethod;
    object?[] IXunitTest.TestMethodArguments => this.ParentTestCase.TestMethodArguments ?? [];
    int IXunitTest.Timeout => 0;
}
