using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class LambdaTaleDiscoverer(ScenarioTestAssembly assembly)
    : TestFrameworkDiscoverer<CombinedTestClass>(assembly)
{
    private ScenarioTestAssembly ScenarioTestAssembly { get; } = assembly;

    private readonly ScenarioDiscoverer scenarioDiscoverer = new(assembly);
    private readonly XunitTestFrameworkDiscoverer xunitTestFrameworkDiscoverer =
        new(new XunitTestAssembly(assembly.Assembly, assembly.ConfigFilePath,
            assembly.Assembly.GetName().Version));

    protected override ValueTask<CombinedTestClass> CreateTestClass(Type @class) =>
        new(new CombinedTestClass(this.ScenarioTestAssembly.Assembly, @class));

    protected override async ValueTask<bool> FindTestsForType(
        CombinedTestClass testClass,
        ITestFrameworkDiscoveryOptions discoveryOptions,
        Func<ITestCase, ValueTask<bool>> discoveryCallback)
    {
        await this.scenarioDiscoverer.Find(discoveryCallback, discoveryOptions);
        await this.xunitTestFrameworkDiscoverer.Find(discoveryCallback, discoveryOptions);
        return true;
    }
    protected override Type[] GetExportedTypes() => this.ScenarioTestAssembly.Assembly.ExportedTypes.ToArray();
}

public class CombinedTestClass(Assembly assembly, Type @class) : ITestClass
{
    public MethodInfo[] Methods => @class.GetMethods();

    public string TestClassName { get; }
    public string? TestClassNamespace { get; }
    public string TestClassSimpleName { get; }
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits { get; }
    public string UniqueID { get; }
    public ITestCollection TestCollection { get; }
}
