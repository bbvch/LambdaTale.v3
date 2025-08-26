using System.Reflection;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class CombinedTestFramework : TestFramework
{
    protected override ITestFrameworkDiscoverer CreateDiscoverer(Assembly assembly) =>
        new LambdaTaleDiscoverer(new ScenarioTestAssembly(assembly));

    protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly) =>
        new ScenarioExecutor(new ScenarioTestAssembly(assembly));

    public override string TestFrameworkDisplayName => "LambdaTale Combined Test Framework";
}
