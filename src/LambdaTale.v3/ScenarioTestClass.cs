using System.ComponentModel;
using System.Reflection;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioTestClass : ITestClass, IXunitSerializable
{
    internal static BindingFlags MethodBindingFlags = BindingFlags.Instance | BindingFlags.Static |
                                                      BindingFlags.Public | BindingFlags.NonPublic |
                                                      BindingFlags.FlattenHierarchy;

    private Type? @class;
    private ScenarioTestAssembly? testAssembly;
    private readonly Lazy<ScenarioTestCollection> testCollection;
    private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> traits;
    private readonly Lazy<string> uniqueID;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioTestClass()
    {
        this.testCollection = new(() =>
            new ScenarioTestCollection(this.TestAssembly, this.Class.FullName ?? this.Class.Name));
        this.traits =
            new(() => ExtensibilityPointFactory.GetClassTraits(this.Class, this.ScenarioTestCollection.Traits));
        this.uniqueID = new(() => UniqueIDGenerator.ForTestClass(this.TestAssembly.UniqueID, this.TestClassName));
    }

#pragma warning disable CS0618 // Type or member is obsolete
    public ScenarioTestClass(ScenarioTestAssembly scenarioTestAssembly, Type scenarioTestClass) : this()
#pragma warning restore CS0618 // Type or member is obsolete
    {
        this.@class = Guard.ArgumentNotNull(scenarioTestClass);
        this.testAssembly = Guard.ArgumentNotNull(scenarioTestAssembly);
    }

    public Type Class =>
        this.@class ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioTestClass)}.{nameof(this.Class)}");

    public MethodInfo[] Methods => this.Class.GetMethods(MethodBindingFlags);

    public ScenarioTestAssembly TestAssembly =>
        this.testAssembly ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioTestClass)}.{nameof(this.TestAssembly)}");

    public string TestClassName =>
        this.Class.FullName ?? throw new InvalidOperationException("Test class must have a full name");

    public string? TestClassNamespace => this.Class.Namespace;

    public string TestClassSimpleName => this.Class.ToSimpleName();

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.traits.Value;

    public string UniqueID => this.uniqueID.Value;

    public ScenarioTestCollection ScenarioTestCollection => this.testCollection.Value;

    ITestCollection ITestClass.TestCollection => this.testCollection.Value;

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.testAssembly = Guard.NotNull("Could not retrieve TestAssembly from serialization",
            info.GetValue<ScenarioTestAssembly>("a"));
        var typeName = Guard.NotNull("Could not retrieve TestClassName from serialization", info.GetValue<string>("c"));
        this.@class =
            Guard.NotNull(
                () => $"Failed to deserialize type '{typeName}' in assembly '{this.testAssembly.AssemblyName}'",
                TypeHelper.GetType(this.testAssembly.AssemblyName, typeName));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("a", this.TestAssembly);
        info.AddValue("c", this.TestClassName);
    }
}
