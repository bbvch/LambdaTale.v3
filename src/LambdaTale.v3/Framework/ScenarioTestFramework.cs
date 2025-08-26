using System.Reflection;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class ScenarioTestFramework : TestFramework
{
    protected override ITestFrameworkDiscoverer CreateDiscoverer(Assembly assembly) =>
        new ScenarioDiscoverer(new ScenarioTestAssembly(assembly));

    protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly) => throw new NotImplementedException();

    public override string TestFrameworkDisplayName => "LambdaTale Framework";
}
