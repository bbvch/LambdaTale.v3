using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class ScenarioExecutor(ScenarioTestAssembly testAssembly)
    : TestFrameworkExecutor<ITestCase>(testAssembly)
{
    public new ScenarioTestAssembly TestAssembly { get; } = testAssembly;

    protected override ITestFrameworkDiscoverer CreateDiscoverer() =>
        new ScenarioDiscoverer(this.TestAssembly);

    public override ValueTask RunTestCases(
        IReadOnlyCollection<ITestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        CancellationToken cancellationToken)
    {
        List<ITestCase> xunitTestCases = new();
        List<ITestCase> lambdaTaleTestCases = new();
        foreach (var testCase in testCases)
        {
            if (testCase is ScenarioTestCase scenarioTestCase)
            {
                lambdaTaleTestCases.Add(scenarioTestCase);
            }
            else
            {
                xunitTestCases.Add(testCase);
            }

        }
        return new ValueTask();
    }
}
