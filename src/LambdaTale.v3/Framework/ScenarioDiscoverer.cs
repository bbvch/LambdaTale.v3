using System.Reflection;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class ScenarioDiscoverer(ScenarioTestAssembly scenarioTestAssembly)
    : TestFrameworkDiscoverer<ScenarioTestClass>(scenarioTestAssembly)
{
    private ScenarioTestAssembly ScenarioTestAssembly { get; } = scenarioTestAssembly;

    protected override ValueTask<ScenarioTestClass> CreateTestClass(Type @class) =>
        new(new ScenarioTestClass(this.ScenarioTestAssembly, @class));

    protected override async ValueTask<bool> FindTestsForType(
        ScenarioTestClass testClass,
        ITestFrameworkDiscoveryOptions discoveryOptions,
        Func<ITestCase, ValueTask<bool>> discoveryCallback)
    {
        foreach (var method in testClass.Methods)
        {
            var attr = method.GetCustomAttributes<ScenarioAttribute>().FirstOrDefault();
            if (attr is null)
            {
                continue;
            }

            var testMethod = new ScenarioTestMethod(testClass, method);

            try
            {
                if (!await FindTestsForMethod(testMethod, discoveryOptions, discoveryCallback))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                TestContext.Current.SendDiagnosticMessage("Exception during discovery of test class {0}:{1}{2}",
                    testClass.Class.FullName, Environment.NewLine, ex);
            }
        }

        return true;
    }

    public static async ValueTask<bool> FindTestsForMethod(
        ScenarioTestMethod testMethod,
        ITestFrameworkDiscoveryOptions discoveryOptions,
        Func<ScenarioTestCase, ValueTask<bool>> discoveryCallback)
    {
        var testCase = new ScenarioTestCase(testMethod, "asdf");
        // testCase.UniqueID = "asdf";
        var tc2 = new ScenarioTestCase(testMethod, "jklö");
        // tc2.UniqueID = "jklö";
        await discoveryCallback(tc2);
        return await discoveryCallback(testCase);
    }

    protected override Type[] GetExportedTypes() => this.ScenarioTestAssembly.Assembly.ExportedTypes.ToArray();
}
