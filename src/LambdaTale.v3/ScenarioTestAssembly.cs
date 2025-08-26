using System.Reflection;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioTestAssembly : ITestAssembly, IXunitSerializable
{
    private Assembly? assembly;
    private readonly Lazy<string> assemblyName;
    private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> traits;
    private readonly Lazy<string> uniqueID;

    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioTestAssembly()
    {
        this.assemblyName = new(() => this.Assembly.GetName().FullName);
        this.traits = new(() => ExtensibilityPointFactory.GetAssemblyTraits(this.Assembly));
        this.uniqueID = new(() => UniqueIDGenerator.ForAssembly(this.Assembly.Location, this.ConfigFilePath));
    }

#pragma warning disable CS0618 // Type or member is obsolete
    public ScenarioTestAssembly(Assembly assembly, string? configFilePath = null) : this()
#pragma warning restore CS0618 // Type or member is obsolete
    {
        Guard.ArgumentNotNull(assembly);

        this.assembly = assembly;
        this.ConfigFilePath = configFilePath;
    }

    public Assembly Assembly =>
        this.assembly ?? throw new InvalidOperationException(
            $"Attempted to retrieve an uninitialized {nameof(ScenarioTestAssembly)}.{nameof(this.Assembly)}");

    public string AssemblyName => this.assemblyName.Value;

    public string AssemblyPath => this.Assembly.Location;
    public string? ConfigFilePath { get; private set; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.traits.Value;

    public string UniqueID => this.uniqueID.Value;

    public Guid ModuleVersionID => this.Assembly.Modules.First().ModuleVersionId;

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.ConfigFilePath = info.GetValue<string>("c");

        var assemblyPath =
            Guard.NotNull("Could not retrieve AssemblyPath from serialization", info.GetValue<string>("a"));
        this.assembly = Guard.NotNull(() => $"Could not load assembly {assemblyPath}", Assembly.LoadFrom(assemblyPath));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("a", this.AssemblyPath);
        info.AddValue("c", this.ConfigFilePath);
    }
}
