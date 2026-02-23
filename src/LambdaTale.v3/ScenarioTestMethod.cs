using System.ComponentModel;
using System.Reflection;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioTestMethod : ITestMethod, IXunitSerializable
{
    private MethodInfo? method;
    private ScenarioTestClass? scenarioTestClass;

    private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> traits;
    private string? uniqueID;

    public ScenarioTestMethod(ScenarioTestClass scenarioTestClass, MethodInfo method)
    {
        this.scenarioTestClass = scenarioTestClass;
        this.method = method;
        this.traits = new(() => ExtensibilityPointFactory.GetMethodTraits(this.Method, this.ScenarioTestClass.Traits));
        this.uniqueID = UniqueIDGenerator.ForTestMethod(scenarioTestClass.UniqueID, this.method.Name);
    }

    public MethodInfo Method =>
        this.method ?? throw new InvalidOperationException(
            $"Attempted to retrieve an unitialized {nameof(ScenarioTestMethod)}.{nameof(this.Method)}");

    public ScenarioTestClass ScenarioTestClass =>
        this.scenarioTestClass ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioTestMethod)}.{nameof(this.ScenarioTestClass)}");


    public int? MethodArity => this.Method.GetArity();

    public string MethodName => this.Method.Name;

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.traits.Value;

    public string UniqueID => this.uniqueID!;

    public ITestClass TestClass => this.ScenarioTestClass;

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.scenarioTestClass = Guard.NotNull("Could not retrieve TestClass from serialization",
            info.GetValue<ScenarioTestClass>("c"));
        this.uniqueID = Guard.NotNull("", info.GetValue<string>("id"));

        var reflectedType = Guard.NotNull("Could not retrieve the class name of the test method",
            info.GetValue<string>("t"));
        var @class = Guard.NotNull(() => $"Could not look up type {reflectedType}", TypeHelper.GetType(reflectedType));
        var methodName = Guard.NotNull("Could not retrieve the MethodName from serialization",
            info.GetValue<string>("n"));
        this.method =
            Guard.NotNull(() => $"Could not find test method {methodName} on test class {this.scenarioTestClass}",
                @class.GetMethod(methodName, ScenarioTestClass.MethodBindingFlags));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        Guard.NotNull("Method does not appear to come from a reflected type", this.Method.ReflectedType);
        Guard.NotNull("Method's reflected type does not have an assembly qualified name",
            this.Method.ReflectedType.AssemblyQualifiedName);

        info.AddValue("c", this.ScenarioTestClass);
        info.AddValue("t", this.Method.ReflectedType.AssemblyQualifiedName);
        info.AddValue("n", this.MethodName);
        info.AddValue("id", this.UniqueID);
    }
}
