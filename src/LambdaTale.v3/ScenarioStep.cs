using Xunit.Sdk;

namespace LambdaTale.v3;

public sealed class ScenarioStep(ScenarioTestCase parentTestCase) : ITest
{
    public ScenarioTestCase ParentTestCase { get; } = parentTestCase;

    public string TaleDisplayname = "Tale Display Name";

    public Action Lambda { get; set; }

    public string TestDisplayName => this.TestCase.TestCaseDisplayName;

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.TestCase.Traits;

    public string UniqueID => UniqueIDGenerator.ForTest(this.TestCase.UniqueID, 0);
    public ITestCase TestCase => parentTestCase;
}
