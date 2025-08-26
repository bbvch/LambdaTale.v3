using System.Reflection;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class CombinedTestFramework : TestFramework
{
    protected override ITestFrameworkDiscoverer CreateDiscoverer(Assembly assembly) =>
        new LambdaTaleDiscoveryFacade(new(assembly));

    protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly) =>
        new ScenarioExecutor(new(assembly));

    public override string TestFrameworkDisplayName => "LambdaTale Combined Test Framework";
}
