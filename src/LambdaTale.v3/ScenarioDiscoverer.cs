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

            if (dataAttributes.Count == 0)
            {
                if (!attr.SkipTestWithoutData)
                {
                    result.Add(MakeTestCase(discoveryOptions, testMethod, attr, args: null));
                }

                return result;
            }

            if (!dataAttributes.All(d => d.SupportsDiscoveryEnumeration()))
            {
                result.Add(MakeTestCase(discoveryOptions, testMethod, attr, args: null, isDelayEnumerated: true));
                return result;
            }

            await using var disposalTracker = new DisposalTracker();
            foreach (var dataAttr in dataAttributes)
            {
                var rows = await dataAttr.GetData(testMethod.Method, disposalTracker);
                foreach (var row in rows)
                {
                    result.Add(MakeTestCase(
                        discoveryOptions,
                        testMethod,
                        attr,
                        row.GetData(),
                        label: row.Label,
                        displayNameOverride: row.TestDisplayName,
                        skipReasonOverride: row.Skip,
                        disableParallelization: row.DisableParallelization ?? dataAttr.DisableParallelization));
                }
            }

            if (result.Count == 0 && attr.SkipTestWithoutData)
            {
                result.Add(MakeTestCase(
                    discoveryOptions, testMethod, attr, args: null, skipReasonOverride: "No data found for scenario"));
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
                errorMessage: ex.Message)];
        }
    }

    private static ScenarioTestCase MakeTestCase(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ScenarioAttribute attr,
        object?[]? args,
        string? label = null,
        string? displayNameOverride = null,
        string? skipReasonOverride = null,
        bool isDelayEnumerated = false,
        bool disableParallelization = false)
    {
        // The details' UniqueID and ResolvedTestMethod are deliberately unused: ScenarioTestCase
        // derives its own ID, and adopting xunit's would orphan the run history of existing tests.
        var details = TestIntrospectionHelper.GetTestCaseDetails(
            discoveryOptions, testMethod, attr, args, timeout: null, baseDisplayName: null, label);

        return new ScenarioTestCase(
            testMethod,
            testMethodArguments: args,
            testCaseDisplayName: displayNameOverride ?? details.TestCaseDisplayName,
            skipReason: skipReasonOverride ?? details.SkipReason,
            sourceFilePath: details.SourceFilePath,
            sourceLineNumber: details.SourceLineNumber,
            isDelayEnumerated: isDelayEnumerated,
            skipTestWithoutData: attr.SkipTestWithoutData,
            isExplicit: details.Explicit,
            skipExceptions: details.SkipExceptions,
            skipType: details.SkipType,
            skipUnless: details.SkipUnless,
            skipWhen: details.SkipWhen,
            timeout: details.Timeout,
            disableParallelization: disableParallelization);
    }
}
