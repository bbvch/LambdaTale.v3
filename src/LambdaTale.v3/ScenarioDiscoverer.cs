using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioDiscoverer : IXunitTestCaseDiscoverer
{
    public async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
    {
        try
        {
            var attr = (ScenarioAttribute)factAttribute;
            var result = new List<IXunitTestCase>();
            var dataAttributes = testMethod.DataAttributes;

            var methodDisplay = discoveryOptions.MethodDisplayOrDefault();
            var formatter = new DisplayNameFormatter(methodDisplay, discoveryOptions.MethodDisplayOptionsOrDefault());
            var baseDisplayName = methodDisplay == TestMethodDisplay.ClassAndMethod
                ? formatter.Format($"{testMethod.TestClass.TestClassName}.{testMethod.MethodName}")
                : formatter.Format(testMethod.MethodName);

            // Case 1: no data attributes
            if (dataAttributes.Count == 0)
            {
                if (!attr.SkipTestWithoutData)
                {
                    result.Add(MakeTestCase(testMethod, null, testMethod.GetDisplayName(baseDisplayName, null, null, null), attr.Skip, attr));
                }

                return result;
            }

            // Check if all data attributes support enumeration at discovery time
            if (!dataAttributes.All(d => d.SupportsDiscoveryEnumeration()))
            {
                // Case 3: delay-enumerated — all data resolved at execution time
                result.Add(new ScenarioTestCase(
                    testMethod,
                    testMethodArguments: null,
                    testCaseDisplayName: testMethod.GetDisplayName(baseDisplayName, null, null, null),
                    skipReason: attr.Skip,
                    sourceFilePath: attr.SourceFilePath,
                    sourceLineNumber: attr.SourceLineNumber,
                    isDelayEnumerated: true,
                    skipTestWithoutData: attr.SkipTestWithoutData));
                return result;
            }

            // Case 2: enumerate at discovery time
            await using var disposalTracker = new DisposalTracker();
            foreach (var dataAttr in dataAttributes)
            {
                var rows = await dataAttr.GetData(testMethod.Method, disposalTracker);
                foreach (var row in rows)
                {
                    var args = row.GetData();
                    var displayName = row.TestDisplayName
                                      ?? testMethod.GetDisplayName(baseDisplayName, row.Label, args, null);
                    var rowSkip = row.Skip ?? attr.Skip;
                    result.Add(MakeTestCase(testMethod, args, displayName, rowSkip, attr));
                }
            }

            if (result.Count == 0 && attr.SkipTestWithoutData)
            {
                result.Add(MakeTestCase(testMethod, null, null, "No data found for scenario", attr));
            }

            return result;
        }
        catch (Exception ex)
        {
            using var g = new UniqueIDGenerator();
            g.Add(testMethod.UniqueID);
            g.Add("discovery-error");
            var uniqueId = g.Compute();

            return [new ExecutionErrorTestCase(
                testMethod,
                testCaseDisplayName: testMethod.MethodName,
                uniqueID: uniqueId,
                sourceFilePath: null,
                sourceLineNumber: null,
                errorMessage: ex.ToString())];
        }
    }

    private static ScenarioTestCase MakeTestCase(
        IXunitTestMethod testMethod,
        object?[]? args,
        string? displayName,
        string? skipReason,
        ScenarioAttribute attr) =>
        new(testMethod,
            testMethodArguments: args,
            testCaseDisplayName: displayName,
            skipReason: skipReason,
            sourceFilePath: attr.SourceFilePath,
            sourceLineNumber: attr.SourceLineNumber);
}
