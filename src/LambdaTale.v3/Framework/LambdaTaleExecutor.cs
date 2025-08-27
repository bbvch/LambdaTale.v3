using LambdaTale.v3.Execution;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3.Framework;

public class LambdaTaleExecutor(ScenarioTestAssembly testAssembly) :
    TestFrameworkExecutor<ITestCase>(testAssembly)
{
    public new ScenarioTestAssembly TestAssembly { get; } = testAssembly;

    protected override ITestFrameworkDiscoverer CreateDiscoverer() =>
        new LambdaTaleDiscoveryFacade(new(this.TestAssembly.Assembly));

    public override async ValueTask RunTestCases(
        IReadOnlyCollection<ITestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        CancellationToken cancellationToken) =>
        await ScenarioTestAssemblyRunner.Instance.Run(this.TestAssembly, testCases.Cast<ScenarioTestCase>().ToArray(),
            executionMessageSink, executionOptions, cancellationToken);
}
