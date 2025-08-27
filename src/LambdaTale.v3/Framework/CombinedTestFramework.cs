using System.Reflection;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class CombinedTestFramework : TestFramework
{
    private readonly string? configFileName;

    public CombinedTestFramework() { }

    public CombinedTestFramework(string? configFilename) =>
        this.configFileName = configFilename;

    protected override ITestFrameworkDiscoverer CreateDiscoverer(Assembly assembly) =>
        new LambdaTaleDiscoveryFacade(new(assembly, this.configFileName));

    protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly) =>
        new LambdaTaleExecutorFacade(new(assembly, this.configFileName));

    public override string TestFrameworkDisplayName => "LambdaTale Combined Test Framework";
}
