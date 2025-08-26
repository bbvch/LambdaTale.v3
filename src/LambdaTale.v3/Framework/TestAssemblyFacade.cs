using System.Reflection;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class TestAssemblyFacade : ITestAssembly
{
    public TestAssemblyFacade(Assembly assembly, string? configFilePath = null)
    {
        Guard.ArgumentNotNull(assembly);

        this.Assembly = assembly;
        this.AssemblyName = assembly.GetName().FullName;
        this.AssemblyPath = assembly.Location;
        this.UniqueID = UniqueIDGenerator.ForAssembly(this.AssemblyPath, configFilePath);
        this.Traits = ExtensibilityPointFactory.GetAssemblyTraits(this.Assembly);
        this.ConfigFilePath = configFilePath;
        this.ModuleVersionID = assembly.ManifestModule.ModuleVersionId;
    }

    public string AssemblyName { get; }
    public string AssemblyPath { get; }
    public string? ConfigFilePath { get; }
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits { get; }

    public Assembly Assembly { get; }
    public string UniqueID { get; }
    public Guid ModuleVersionID { get; }
}
