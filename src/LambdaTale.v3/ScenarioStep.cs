using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioStep : IXunitTest
{
    public ScenarioStep(
        ScenarioTestCase parentTestCase,
        int stepIndex,
        string tale,
        TaleBody body)
    {
        this.ParentTestCase = parentTestCase;
        this.StepIndex = stepIndex;
        this.Tale = tale;
        this.Body = body;
    }

    public ScenarioTestCase ParentTestCase { get; }
    public int StepIndex { get; }
    public string Tale { get; }
    public TaleBody Body { get; }

    public ITestCase TestCase => this.ParentTestCase;
    public string TestDisplayName => $"[{this.StepIndex}] {this.Tale}";
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
