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
                if (!await FindTestsForMethod(testClass, testMethod, attr, discoveryOptions, discoveryCallback))
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
        ScenarioTestClass testClass,
        ScenarioTestMethod testMethod,
        ScenarioAttribute attr,
        ITestFrameworkDiscoveryOptions discoveryOptions,
        Func<ScenarioTestCase, ValueTask<bool>> discoveryCallback)
    {
        using var ctx = Scenario.Acquire();
        var tc = Activator.CreateInstance(testClass.Class);

        testMethod.Method.Invoke(tc, null);

        var steps = Scenario.TestDefinitions.Select(td =>
        {
            var tci = new ScenarioTestCase(testMethod, td.Tale, td.Lambda, td.index, sourceFilePath: attr.SourceFilePath, sourceLineNumber: attr.SourceLineNumber);
            return (td.index, tci);
        });
        steps = steps.OrderBy(x => x.index);

        foreach (var (_, test) in steps)
        {
            if (!await discoveryCallback(test))
            {
                return false;
            }
        }

        return true;
    }

    protected override Type[] GetExportedTypes() => this.ScenarioTestAssembly.Assembly.ExportedTypes.ToArray();
}
