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
    private int caseIndex;

    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioTestCase()
    {
        this.uniqueId = new(() =>
        {
            using var generator = new UniqueIDGenerator();
            generator.Add(this.scenarioTestMethod!.UniqueID);
            generator.Add(this.tale!);
            generator.Add(this.caseIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return generator.Compute();
        });
    }

    public ScenarioTestCase(
        ScenarioTestMethod scenarioTestMethod,
        string tale,
        int caseIndex,
        string? sourceFilePath = null,
        int? sourceLineNumber = null)
    {
        this.scenarioTestMethod = Guard.ArgumentNotNull(scenarioTestMethod);
        this.tale = Guard.ArgumentNotNullOrEmpty(tale);
        this.caseIndex = caseIndex;
        this.SourceFilePath = sourceFilePath;
        this.SourceLineNumber = sourceLineNumber;
        this.uniqueId = new(() =>
        {
            using var generator = new UniqueIDGenerator();
            generator.Add(this.scenarioTestMethod!.UniqueID);
            generator.Add(this.tale!);
            generator.Add(this.caseIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return generator.Compute();
        });
    }

    public ScenarioTestClass ScenarioTestClass => this.ScenarioTestMethod.ScenarioTestClass;

    public ScenarioTestCollection ScenarioTestCollection =>
        this.ScenarioTestMethod.ScenarioTestClass.ScenarioTestCollection;

    public ScenarioTestMethod ScenarioTestMethod =>
        this.scenarioTestMethod ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioTestCase)}.{nameof(this.ScenarioTestMethod)}");

    public string UniqueID => this.uniqueId.Value;

    public int CaseIndex => this.caseIndex;

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.scenarioTestMethod = Guard.NotNull("Could not retrieve TestMethod from serialization",
            info.GetValue<ScenarioTestMethod>("tm"));
        this.tale = Guard.NotNull("", info.GetValue<string>("tale"));
        this.caseIndex = info.GetValue<int>("ci");
        this.SourceFilePath = info.GetValue<string>("sf");
        this.SourceLineNumber = info.GetValue<int?>("sl");
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("tm", this.ScenarioTestMethod);
        info.AddValue("tale", this.tale);
        info.AddValue("ci", this.caseIndex);
        if (this.SourceFilePath is not null)
        {
            info.AddValue("sf", this.SourceFilePath);
        }

        if (this.SourceLineNumber is not null)
        {
            info.AddValue("sl", this.SourceLineNumber);
        }
    }

    public string TestCaseDisplayName =>
        this.ScenarioTestMethod.MethodName;

    #region StuffIDontCareAboutRightNow

    bool ITestCaseMetadata.Explicit => false;

    string? ITestCaseMetadata.SkipReason => null;

    public string? SourceFilePath { get; private set; }

    public int? SourceLineNumber { get; private set; }

    ITestClass ITestCase.TestClass => this.ScenarioTestClass;

    int? ITestCaseMetadata.TestClassMetadataToken => this.ScenarioTestClass.Class.MetadataToken;

    string ITestCaseMetadata.TestClassName => this.ScenarioTestClass.TestClassName;

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
