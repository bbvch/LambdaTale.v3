using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public sealed class ScenarioDiscoverer : IXunitTestCaseDiscoverer
{
    public async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
    {
        var attr = (ScenarioAttribute)factAttribute;
        var result = new List<IXunitTestCase>();
        var dataAttributes = testMethod.DataAttributes;

        // Case 1: no data attributes
        if (dataAttributes.Count == 0)
        {
            if (!attr.SkipTestWithoutData)
                result.Add(MakeTestCase(testMethod, null, null, attr.Skip, attr));
            return result;
        }

        // Check if all data attributes support enumeration at discovery time
        if (!dataAttributes.All(d => d.SupportsDiscoveryEnumeration()))
        {
            // Case 3: delay-enumerated — all data resolved at execution time
            result.Add(new ScenarioTestCase(
                testMethod,
                testMethodArguments: null,
                testCaseDisplayName: null,
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
                    ?? testMethod.GetDisplayName(testMethod.MethodName, row.Label, args, null);
                var rowSkip = row.Skip ?? attr.Skip;
                result.Add(MakeTestCase(testMethod, args, displayName, rowSkip, attr));
            }
        }

        if (result.Count == 0 && attr.SkipTestWithoutData)
            result.Add(MakeTestCase(testMethod, null, null, "No data found for scenario", attr));

        return result;
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
