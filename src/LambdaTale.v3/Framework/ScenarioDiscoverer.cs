using System.Reflection;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class ScenarioDiscoverer(ScenarioTestAssembly testAssembly)
    : TestFrameworkDiscoverer<ScenarioTestClass>(testAssembly)
{
    public new ScenarioTestAssembly TestAssembly { get; } = testAssembly;

    protected override ValueTask<ScenarioTestClass> CreateTestClass(Type @class) =>
        new(new ScenarioTestClass(this.TestAssembly, @class));

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

    private static async ValueTask<bool> FindTestsForMethod(
        ScenarioTestMethod testMethod,
        ITestFrameworkDiscoveryOptions discoveryOptions,
        Func<ScenarioTestCase, ValueTask<bool>> discoveryCallback)
    {
        var testCase = new ScenarioTestCase(testMethod);
        return await discoveryCallback(testCase);
    }

    protected override Type[] GetExportedTypes() => this.TestAssembly.Assembly.ExportedTypes.ToArray();
}
