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
    private object?[]? testMethodArguments;
    private int dataRowIndex = -1;
    private string? testCaseDisplayName;
    private string? skipReason;

    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioTestCase()
    {
        this.uniqueId = new(() =>
        {
            using var generator = new UniqueIDGenerator();
            generator.Add(this.scenarioTestMethod!.UniqueID);
            generator.Add(this.tale!);
            generator.Add(this.caseIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (this.testMethodArguments is not null)
            {
                foreach (var arg in this.testMethodArguments)
                {
                    generator.Add(SerializationHelper.Instance.Serialize(arg));
                }
            }

            return generator.Compute();
        });
    }

    public ScenarioTestCase(
        ScenarioTestMethod scenarioTestMethod,
        string tale,
        int caseIndex,
        object?[]? testMethodArguments = null,
        int dataRowIndex = -1,
        string? testCaseDisplayName = null,
        string? sourceFilePath = null,
        int? sourceLineNumber = null,
        string? skipReason = null)
    {
        this.scenarioTestMethod = Guard.ArgumentNotNull(scenarioTestMethod);
        this.tale = Guard.ArgumentNotNullOrEmpty(tale);
        this.caseIndex = caseIndex;
        this.testMethodArguments = testMethodArguments;
        this.dataRowIndex = dataRowIndex;
        this.testCaseDisplayName = testCaseDisplayName;
        this.SourceFilePath = sourceFilePath;
        this.SourceLineNumber = sourceLineNumber;
        this.skipReason = skipReason;
        this.uniqueId = new(() =>
        {
            using var generator = new UniqueIDGenerator();
            generator.Add(this.scenarioTestMethod!.UniqueID);
            generator.Add(this.tale!);
            generator.Add(this.caseIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (this.testMethodArguments is not null)
            {
                foreach (var arg in this.testMethodArguments)
                {
                    generator.Add(SerializationHelper.Instance.Serialize(arg));
                }
            }

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

    public object?[]? TestMethodArguments => this.testMethodArguments;

    public int DataRowIndex => this.dataRowIndex;

    public string? SkipReason => this.skipReason;

    public string TestCaseDisplayName =>
        this.testCaseDisplayName ?? this.scenarioTestMethod!.MethodName;

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.scenarioTestMethod = Guard.NotNull("Could not retrieve TestMethod from serialization",
            info.GetValue<ScenarioTestMethod>("tm"));
        this.tale = Guard.NotNull("", info.GetValue<string>("tale"));
        this.caseIndex = info.GetValue<int>("ci");
        this.SourceFilePath = info.GetValue<string>("sf");
        this.SourceLineNumber = info.GetValue<int?>("sl");
        this.dataRowIndex = info.GetValue<int>("dri");
        var argc = info.GetValue<int>("argc");
        if (argc >= 0)
        {
            this.testMethodArguments = new object?[argc];
            for (var i = 0; i < argc; i++)
            {
                var serialized = info.GetValue<string>($"arg{i}");
                this.testMethodArguments[i] = SerializationHelper.Instance.Deserialize(serialized!);
            }
        }
        this.testCaseDisplayName = info.GetValue<string?>("dn");
        this.skipReason = info.GetValue<string?>("sr");
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
        info.AddValue("dri", this.dataRowIndex);
        var argc = this.testMethodArguments?.Length ?? -1;
        info.AddValue("argc", argc);
        if (this.testMethodArguments is not null)
        {
            for (var i = 0; i < this.testMethodArguments.Length; i++)
            {
                info.AddValue($"arg{i}", SerializationHelper.Instance.Serialize(this.testMethodArguments[i]));
            }
        }

        if (this.testCaseDisplayName is not null)
        {
            info.AddValue("dn", this.testCaseDisplayName);
        }

        if (this.skipReason is not null)
        {
            info.AddValue("sr", this.skipReason);
        }
    }

    #region StuffIDontCareAboutRightNow

    bool ITestCaseMetadata.Explicit => false;

    string? ITestCaseMetadata.SkipReason => this.skipReason;

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
