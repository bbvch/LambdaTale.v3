using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioStep : IXunitTest
{
    public ScenarioStep(ScenarioTestCase parentTestCase, int stepIndex, string tale, TaleBody body)
    {
        ParentTestCase = parentTestCase;
        StepIndex = stepIndex;
        Tale = tale;
        Body = body;
    }

    public ScenarioTestCase ParentTestCase { get; }
    public int StepIndex { get; }
    public string Tale { get; }
    public TaleBody Body { get; }

    // ITest
    public ITestCase TestCase => ParentTestCase;
    public string TestDisplayName => $"[{StepIndex}] {Tale}";
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => ParentTestCase.Traits;
    public string UniqueID => UniqueIDGenerator.ForTest(ParentTestCase.UniqueID, StepIndex);

    // IXunitTest
    IXunitTestCase IXunitTest.TestCase => ParentTestCase;
    bool IXunitTest.Explicit => false;
    string? IXunitTest.SkipReason => null;
    Type? IXunitTest.SkipType => null;
    string? IXunitTest.SkipUnless => null;
    string? IXunitTest.SkipWhen => null;
    IXunitTestMethod IXunitTest.TestMethod => ParentTestCase.TestMethod;
    object?[] IXunitTest.TestMethodArguments => ParentTestCase.TestMethodArguments ?? [];
    int IXunitTest.Timeout => 0;
}
