using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Tests.Harness;

// Fixtures passed in as TFixture should be `private sealed` nested classes so xUnit's
// own discovery skips them. The fixture method does NOT need [Scenario] — Run() invokes
// the MethodInfo directly.
internal static class ScenarioTestRunner
{
    public static async Task<CapturingMessageBus> RunFixture<TFixture>(
        string methodName,
        object?[]? testMethodArguments = null,
        string? testCaseDisplayName = null,
        string? skipReason = null,
        bool @explicit = false,
        ExplicitOption explicitOption = ExplicitOption.Off,
        Type[]? skipExceptions = null,
        Type? skipType = null,
        string? skipUnless = null,
        string? skipWhen = null,
        int timeout = 0,
        bool isDelayEnumerated = false,
        object?[]? constructorArguments = null)
    {
        var bus = new CapturingMessageBus();
        var testMethod = FixtureMethod.For<TFixture>(methodName);
        var testCase = new ScenarioTestCase(
            testMethod,
            testMethodArguments: testMethodArguments,
            testCaseDisplayName: testCaseDisplayName,
            skipReason: skipReason,
            isExplicit: @explicit,
            skipExceptions: skipExceptions,
            skipType: skipType,
            skipUnless: skipUnless,
            skipWhen: skipWhen,
            timeout: timeout,
            isDelayEnumerated: isDelayEnumerated);

        await using var scheduler = ExecutionScheduler.CreateUnlimited();
        await using var methodFixtures = new FixtureMappingManager("Method");

        await testCase.Run(
            explicitOption,
            bus,
            constructorArguments: constructorArguments ?? [],
            new ExceptionAggregator(),
            new CancellationTokenSource(),
            ParallelMode.None,
            scheduler,
            methodFixtures);
        return bus;
    }
}
