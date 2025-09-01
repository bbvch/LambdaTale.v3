using System.ComponentModel;
using System.Diagnostics;
using Xunit.Internal;
using Xunit.Sdk;

namespace LambdaTale.v3;

[DebuggerDisplay(
    @"\{ class = {ScenarioTestMethod.TestClass.TestClassName}, method = {ScenarioTestMethod.Method.Name}, display = {TestCaseDisplayName} \}")]
public sealed class ScenarioTestCase : ITestCase, IXunitSerializable
{
    private ScenarioTestMethod? scenarioTestMethod;
    private string? tale;
    private Lazy<string> uniqueId;
    private readonly TaleBody body;
    private readonly int caseIndex;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioTestCase()
    {
        this.body = new TaleBody.SynchronousTaleBody(() => { });
        this.uniqueId = new(() =>
        {
            using var generator = new UniqueIDGenerator();
            var baseId = UniqueIDGenerator.ForTestCase(this.scenarioTestMethod!.UniqueID, null, null);
            generator.Add(baseId);
            generator.Add(new Random().Next().ToString()); // WTF??
            generator.Add(this.tale!);
            return generator.Compute();
        });
    }

#pragma warning disable CS0618 // Type or member is obsolete
    public ScenarioTestCase(
        ScenarioTestMethod scenarioTestMethod,
        string tale,
        TaleBody body,
        int caseIndex) : this()
#pragma warning restore CS0618 // Type or member is obsolete
    {
        this.scenarioTestMethod = Guard.ArgumentNotNull(scenarioTestMethod);
        this.tale = Guard.ArgumentNotNullOrEmpty(tale);
        this.body = Guard.ArgumentNotNull(body);
        this.caseIndex = caseIndex;
    }

    public ScenarioTestClass ScenarioTestClass => this.ScenarioTestMethod.ScenarioTestClass;

    public ScenarioTestCollection ScenarioTestCollection =>
        this.ScenarioTestMethod.ScenarioTestClass.ScenarioTestCollection;

    public ScenarioTestMethod ScenarioTestMethod =>
        this.scenarioTestMethod ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioTestCase)}.{nameof(this.ScenarioTestMethod)}");

    public string UniqueID => this.uniqueId.Value;

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.scenarioTestMethod = Guard.NotNull("Could not retrieve TestMethod from serialization",
            info.GetValue<ScenarioTestMethod>("tm"));
        this.tale = Guard.NotNull("", info.GetValue<string>("seed"));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("tm", this.ScenarioTestMethod);
        info.AddValue("seed", this.tale);
    }


    // TODO: db: ? Why does the test explorer show the first testcase name as the method name...
    public string TestCaseDisplayName => $"[{this.caseIndex}]: {this.ScenarioTestMethod.MethodName}: {this.tale}";

    public ValueTask<IReadOnlyCollection<ScenarioStep>> CreateSteps() =>
        new([
            new ScenarioStep(this, this.tale ?? string.Empty, this.body, this.caseIndex)
        ]);

    #region StuffIDontCareAboutRightNow

    bool ITestCaseMetadata.Explicit => false;

    string? ITestCaseMetadata.SkipReason => null;

    string? ITestCaseMetadata.SourceFilePath => null;

    int? ITestCaseMetadata.SourceLineNumber => null;

    ITestClass ITestCase.TestClass => this.ScenarioTestClass;

    int? ITestCaseMetadata.TestClassMetadataToken => this.ScenarioTestClass.Class.MetadataToken;

    string ITestCaseMetadata.TestClassName => this.ScenarioTestClass.Class.Name;

    string? ITestCaseMetadata.TestClassNamespace => this.ScenarioTestClass.Class.Namespace;

    string ITestCaseMetadata.TestClassSimpleName => this.ScenarioTestClass.TestClassSimpleName;

    ITestMethod ITestCase.TestMethod => this.ScenarioTestMethod;

    public int? TestMethodArity => this.ScenarioTestMethod.MethodArity;

    int? ITestCaseMetadata.TestMethodMetadataToken => this.ScenarioTestMethod.Method.MetadataToken;

    string ITestCaseMetadata.TestMethodName => this.ScenarioTestMethod.Method.Name;

    string[]? ITestCaseMetadata.TestMethodParameterTypesVSTest => null;

    string? ITestCaseMetadata.TestMethodReturnTypeVSTest => null;

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.ScenarioTestMethod.Traits;

    ITestCollection ITestCase.TestCollection => this.ScenarioTestCollection;

    #endregion
}
