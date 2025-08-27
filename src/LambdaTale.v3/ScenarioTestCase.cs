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
    private string? seed;
    private Lazy<string> uniqueId;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioTestCase()
    {
        this.uniqueId = new(() =>
        {
            using var generator = new UniqueIDGenerator();
            var baseId = UniqueIDGenerator.ForTestCase(this.scenarioTestMethod!.UniqueID, null, null);
            generator.Add(baseId);
            generator.Add(new Random().Next().ToString()); // WTF??
            generator.Add(this.seed!);
            return generator.Compute();
        });
    }

#pragma warning disable CS0618 // Type or member is obsolete
    public ScenarioTestCase(ScenarioTestMethod scenarioTestMethod, string caseIndex) : this()
#pragma warning restore CS0618 // Type or member is obsolete
    {
        this.scenarioTestMethod = Guard.ArgumentNotNull(scenarioTestMethod);
        this.seed = Guard.ArgumentNotNullOrEmpty(caseIndex);
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
        this.seed = Guard.NotNull("", info.GetValue<string>("seed"));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("tm", this.ScenarioTestMethod);
        info.AddValue("seed", this.seed);
    }


    public string TestCaseDisplayName => //this.ScenarioTestMethod.Method.GetDisplayNameWithArguments(this.ScenarioTestMethod.MethodName, [this.seed], null);
        $"{this.ScenarioTestMethod.MethodName}: {this.seed}";

    public ValueTask<IReadOnlyCollection<ScenarioStep>> CreateSteps() =>
        // TODO: Create the actual steps
        new([
            new ScenarioStep(this, "Step 1", () => Console.WriteLine("Hello, Step 1!"), 0),
            new ScenarioStep(this, "Step 2", () => Console.WriteLine("Hello, Step 2!"), 1),
            new ScenarioStep(this, "Boom", () => throw new NotImplementedException(), 2)
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
