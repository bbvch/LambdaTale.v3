using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class LambdaTaleExecutorFacade(TestAssemblyFacade testAssembly)
    : TestFrameworkExecutor<ITestCase>(testAssembly)
{
    private new TestAssemblyFacade TestAssembly { get; } = testAssembly;

    private readonly XunitTestFrameworkExecutor xunitExecutor = new(
        new XunitTestAssembly(testAssembly.Assembly, testAssembly.ConfigFilePath,
            testAssembly.Assembly.GetName().Version, testAssembly.UniqueID));

    protected override ITestFrameworkDiscoverer CreateDiscoverer() =>
        new LambdaTaleDiscoveryFacade(this.TestAssembly);

    public override async ValueTask RunTestCases(
        IReadOnlyCollection<ITestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        CancellationToken cancellationToken)
    {
        List<IXunitTestCase> xunitTestCases = new();
        List<ITestCase> lambdaTaleTestCases = new();
        foreach (var testCase in testCases)
        {
            if (testCase is ScenarioTestCase scenarioTestCase)
            {
                lambdaTaleTestCases.Add(scenarioTestCase);
            }
            else
            {
                xunitTestCases.Add((IXunitTestCase)testCase); // TODO: This is bad :)
            }
        }
        await this.xunitExecutor.RunTestCases(
             xunitTestCases, executionMessageSink, executionOptions, cancellationToken);
    }
}
