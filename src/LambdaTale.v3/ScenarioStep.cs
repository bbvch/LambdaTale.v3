using Xunit.Sdk;

namespace LambdaTale.v3;

public sealed class ScenarioStep(ScenarioTestCase parentTestCase, string tale, Action lambda) : ITest
{
    public ScenarioTestCase ParentTestCase { get; } = parentTestCase;

    public Action Lambda { get; } = lambda;

    public string TestDisplayName { get; } = tale;

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.ParentTestCase.Traits;

    public string UniqueID => UniqueIDGenerator.ForTest(this.ParentTestCase.UniqueID, 0);
    ITestCase ITest.TestCase => this.ParentTestCase;
}
