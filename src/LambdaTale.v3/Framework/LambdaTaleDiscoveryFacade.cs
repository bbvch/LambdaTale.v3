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
