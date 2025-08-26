using System.Reflection;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class LambdaTaleDiscoveryFacade(TestAssemblyFacade assemblyFacade)
    : TestFrameworkDiscoverer<ITestClass>(assemblyFacade)
{
    private readonly ScenarioDiscoverer lambdaTaleDiscoverer = new(new ScenarioTestAssembly(
        assemblyFacade.Assembly, assemblyFacade.ConfigFilePath));

    private readonly XunitTestFrameworkDiscoverer xunitDiscoverer =
        new(new XunitTestAssembly(assemblyFacade.Assembly, assemblyFacade.ConfigFilePath,
            assemblyFacade.Assembly.GetName().Version));

    protected override ValueTask<ITestClass> CreateTestClass(Type @class) =>
        throw new InvalidOperationException("This method should never be called.");

    protected override ValueTask<bool> FindTestsForType(
        ITestClass testClass,
        ITestFrameworkDiscoveryOptions discoveryOptions,
        Func<ITestCase, ValueTask<bool>> discoveryCallback) =>
        throw new InvalidOperationException("This method should never be called.");

    public override async ValueTask Find(
        Func<ITestCase, ValueTask<bool>> callback,
        ITestFrameworkDiscoveryOptions discoveryOptions,
        Type[]? types = null,
        CancellationToken? cancellationToken = null)
    {
        await this.lambdaTaleDiscoverer.Find(callback, discoveryOptions, types, cancellationToken);
        await this.xunitDiscoverer.Find(callback, discoveryOptions, types, cancellationToken);
    }

    protected override Type[] GetExportedTypes() => assemblyFacade.Assembly.ExportedTypes.ToArray();
}

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
