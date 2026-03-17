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
        var dataAttributes = ExtensibilityPointFactory.GetMethodDataAttributes(testMethod.Method).ToList();

        if (dataAttributes.Count == 0)
        {
            if (attr.SkipTestWithoutData)
                return true;

            return await InvokeAndEmit(
                testClass, testMethod, attr,
                args: null, dataRowIndex: -1, methodDisplayName: null,
                discoveryCallback);
        }

        await using var disposalTracker = new DisposalTracker();
        var rowIndex = 0;
        foreach (var dataAttr in dataAttributes)
        {
            var rows = await dataAttr.GetData(testMethod.Method, disposalTracker);
            foreach (var row in rows)
            {
                var args = row.GetData();
                var methodDisplayName = row.TestDisplayName
                                        ?? testMethod.Method.GetDisplayNameWithArguments(testMethod.MethodName, args, null);

                if (!await InvokeAndEmit(
                        testClass, testMethod, attr,
                        args, rowIndex, methodDisplayName,
                        discoveryCallback))
                {
                    return false;
                }

                rowIndex++;
            }
        }

        return true;
    }

    protected override Type[] GetExportedTypes() => this.ScenarioTestAssembly.Assembly.ExportedTypes.ToArray();


    private static async ValueTask<bool> InvokeAndEmit(
        ScenarioTestClass testClass,
        ScenarioTestMethod testMethod,
        ScenarioAttribute attr,
        object?[]? args,
        int dataRowIndex,
        string? methodDisplayName,
        Func<ScenarioTestCase, ValueTask<bool>> discoveryCallback)
    {
        using var ctx = Scenario.Acquire();
        var tc = Activator.CreateInstance(testClass.Class);
        var parameterValues = args ?? DefaultParameterValues(testMethod.Method);

        testMethod.Method.Invoke(tc, parameterValues);

        var steps = Scenario.TestDefinitions
            .OrderBy(td => td.index)
            .Select(td => new ScenarioTestCase(
                testMethod, td.Tale, td.index,
                testMethodArguments: args,
                dataRowIndex: dataRowIndex,
                testCaseDisplayName: methodDisplayName,
                sourceFilePath: attr.SourceFilePath,
                sourceLineNumber: attr.SourceLineNumber));

        foreach (var test in steps)
        {
            if (!await discoveryCallback(test))
            {
                return false;
            }
        }

        return true;
    }

    private static object?[] DefaultParameterValues(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var values = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            values[i] = parameters[i].ParameterType.GetDefaultValue();
        }

        return values;
    }
}
