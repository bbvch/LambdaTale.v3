using System.ComponentModel;
using System.Diagnostics;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

[DebuggerDisplay(@"\{ class = {ScenarioTestMethod.TestClass.Class.Name}, method = {ScenarioTestMethod.Method.Name}, display = {TestCaseDisplayName} \}")]
public sealed class ScenarioTestCase : ITestCase, IXunitSerializable
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioTestCase()
    {
    }

    public ScenarioTestCase(IXunitTestMethod scenarioTestMethod)
    {
        this.ScenarioTestMethod = Guard.ArgumentNotNull(scenarioTestMethod);
    }

    public IXunitTestClass ScenarioTestClass => this.ScenarioTestMethod.TestClass;

    public IXunitTestCollection ScenarioTestCollection => this.ScenarioTestMethod.TestClass.TestCollection;

    public IXunitTestMethod ScenarioTestMethod { get; set; }

    public string UniqueID => UniqueIDGenerator.ForTestCase(this.ScenarioTestMethod.UniqueID,
        testMethodGenericTypes: null, testMethodArguments: null);

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.ScenarioTestMethod = Guard.NotNull("Could not retrieve TestMethod from serialization",
            info.GetValue<XunitTestMethod>("tm"));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("tm", this.ScenarioTestMethod);
    }


    #region StuffIDontCareAboutRightNow

    bool ITestCaseMetadata.Explicit => false;

    string? ITestCaseMetadata.SkipReason => null;

    string? ITestCaseMetadata.SourceFilePath => null;

    string ITestCaseMetadata.TestCaseDisplayName =>
        $"{this.ScenarioTestClass.TestClassName};{this.ScenarioTestMethod.MethodName}";

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
