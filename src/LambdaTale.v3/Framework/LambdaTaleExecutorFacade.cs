using LambdaTale.v3.Execution;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class LambdaTaleExecutorFacade(TestAssemblyFacade testAssembly)
    : TestFrameworkExecutor<ITestCase>(testAssembly)
{
    public ScenarioTestAssembly LambdaTaleAssembly { get; } = new(
        testAssembly.Assembly, testAssembly.ConfigFilePath);

    public IXunitTestAssembly XunitTestAssembly { get; } = new XunitTestAssembly(
        testAssembly.Assembly, testAssembly.ConfigFilePath, testAssembly.Assembly.GetName().Version,
        testAssembly.UniqueID);

    protected override ITestFrameworkDiscoverer CreateDiscoverer() =>
        new LambdaTaleDiscoveryFacade(testAssembly);

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

        var testSummary = await RunLambdaTaleTests(this.LambdaTaleAssembly,
            lambdaTaleTestCases.Cast<ScenarioTestCase>().ToArray(),
            executionMessageSink, executionOptions, cancellationToken);

        testSummary.Aggregate(await RunXunitTests(this.XunitTestAssembly, xunitTestCases,
            executionMessageSink, executionOptions, cancellationToken));

        using IMessageBus messageBus = executionOptions.SynchronousMessageReportingOrDefault()
            ? new SynchronousMessageBus(executionMessageSink, executionOptions.StopOnTestFailOrDefault())
            : new MessageBus(executionMessageSink, executionOptions.StopOnTestFailOrDefault());

        _ = messageBus.QueueMessage(new TestAssemblyFinished
        {
            AssemblyUniqueID = this.TestAssembly.UniqueID,
            FinishTime = DateTimeOffset.Now,
            ExecutionTime = testSummary.Time,
            TestsFailed = testSummary.Failed,
            TestsNotRun = testSummary.NotRun,
            TestsSkipped = testSummary.Skipped,
            TestsTotal = testSummary.Total,
        });
    }

    private static async ValueTask<RunSummary> RunXunitTests(
        IXunitTestAssembly testAssembly,
        IReadOnlyCollection<IXunitTestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        CancellationToken cancellationToken) =>
        await XunitAssemblyRunnerWrapper.Instance.Run(testAssembly, testCases, executionMessageSink, executionOptions,
            cancellationToken);


    private static async ValueTask<RunSummary> RunLambdaTaleTests(
        ScenarioTestAssembly testAssembly,
        ScenarioTestCase[] testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        CancellationToken cancellationToken) =>
        await ScenarioTestAssemblyRunner.Instance.Run(testAssembly, testCases, executionMessageSink,
            executionOptions,
            cancellationToken);
}
