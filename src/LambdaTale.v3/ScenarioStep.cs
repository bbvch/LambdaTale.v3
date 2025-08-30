using System.ComponentModel;
using Xunit.Internal;
using Xunit.Sdk;

namespace LambdaTale.v3;

public sealed class ScenarioStep : ITest, IXunitSerializable
{
    private ScenarioTestCase? parentTestCase;
    private Action? lambda;
    private readonly int testIndex;
    private string? tale;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioStep()
    {
    }

    public ScenarioStep(ScenarioTestCase parentTestCase, string tale, Action lambda, int testIndex)
    {
        this.parentTestCase = parentTestCase;
        this.lambda = lambda;
        this.testIndex = testIndex;
        this.tale = tale;
    }

    public ScenarioTestCase ParentTestCase =>
        this.parentTestCase ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioStep)}.{nameof(this.ParentTestCase)}");

    public Action Lambda =>
        this.lambda ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioStep)}.{nameof(this.Lambda)}");

    // TODO: db: ? How can we get the steps to use this name instead of the testcase (in the test explorer)
    public string TestDisplayName => $"[{this.testIndex}]: {this.ParentTestCase.ScenarioTestMethod.Method.Name}.{this.tale}";

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.ParentTestCase.Traits;

    public string UniqueID => UniqueIDGenerator.ForTest(this.ParentTestCase.UniqueID, this.testIndex);
    ITestCase ITest.TestCase => this.ParentTestCase;

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.parentTestCase = Guard.NotNull("", info.GetValue<ScenarioTestCase>("ptc"));
        this.tale = Guard.NotNull("", info.GetValue<string>("tale"));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("ptc", this.ParentTestCase);
        info.AddValue("tale", this.TestDisplayName);
    }
}
