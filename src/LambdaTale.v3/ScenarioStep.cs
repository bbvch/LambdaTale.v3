using System.ComponentModel;
using Xunit.Internal;
using Xunit.Sdk;

namespace LambdaTale.v3;

public sealed class ScenarioStep : ITest, IXunitSerializable
{
    private ScenarioTestCase? parentTestCase;
    private TaleBody? body;
    private string? tale;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioStep()
    {
    }

    public ScenarioStep(ScenarioTestCase parentTestCase, string tale, TaleBody body)
    {
        this.parentTestCase = parentTestCase;
        this.body = body;
        this.tale = tale;
    }

    public ScenarioTestCase ParentTestCase =>
        this.parentTestCase ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioStep)}.{nameof(this.ParentTestCase)}");

    public TaleBody Body =>
        this.body ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioStep)}.{nameof(this.Body)}");

    public string TestDisplayName =>
        this.ParentTestCase.DataRowIndex >= 0
            ? $"{this.ParentTestCase.TestCaseDisplayName} [{this.ParentTestCase.CaseIndex}] {this.tale}"
            : $"[{this.ParentTestCase.CaseIndex}] {this.tale}";

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.ParentTestCase.Traits;

    public string UniqueID => UniqueIDGenerator.ForTest(this.ParentTestCase.UniqueID, this.ParentTestCase.CaseIndex);
    ITestCase ITest.TestCase => this.ParentTestCase;

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.parentTestCase = Guard.NotNull("", info.GetValue<ScenarioTestCase>("ptc"));
        this.tale = Guard.NotNull("", info.GetValue<string>("tale"));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("ptc", this.ParentTestCase);
        info.AddValue("tale", this.tale);
    }
}
