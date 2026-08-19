using LambdaTale.v3.Tests.Harness;
using Xunit;

namespace LambdaTale.v3.Tests.Behavior;

public class TestOutputHelperBehaviorTests
{
    // Mirrors how xUnit's class runner resolves an ITestOutputHelper constructor
    // parameter: it supplies a deferred Func<ITestOutputHelper> placeholder.
    private static object?[] OutputHelperConstructorArguments =>
        [(Func<ITestOutputHelper>)(() => TestContext.Current.TestOutputHelper!)];

    [Fact]
    public async Task OutputWrittenInStepIsCapturedOnStepResult()
    {
        var bus = await ScenarioTestRunner.RunFixture<OutputFixture>(
            nameof(OutputFixture.ScenarioWritesOutput),
            constructorArguments: OutputHelperConstructorArguments);

        var passed = bus.AssertStepPassed("Given a step that writes output");
        Assert.Contains("hello from step", passed.Output);
    }

    [Fact]
    public async Task OutputWrittenViaTestContextIsCapturedOnStepResult()
    {
        var bus = await ScenarioTestRunner.RunFixture<TestContextOutputFixture>(
            nameof(TestContextOutputFixture.ScenarioWritesOutput));

        var passed = bus.AssertStepPassed("Given a step that writes via TestContext");
        Assert.Contains("ambient output", passed.Output);
    }

    [Fact]
    public async Task RunningAScenarioDoesNotLeakTestContext()
    {
        var before = TestContext.Current.TestOutputHelper;

        await ScenarioTestRunner.RunFixture<TestContextOutputFixture>(
            nameof(TestContextOutputFixture.ScenarioWritesOutput));

        Assert.Same(before, TestContext.Current.TestOutputHelper);
    }

    private sealed class OutputFixture(ITestOutputHelper output)
    {
        public void ScenarioWritesOutput() =>
            "Given a step that writes output".x(() => output.WriteLine("hello from step"));
    }

    private sealed class TestContextOutputFixture
    {
        public void ScenarioWritesOutput() =>
            "Given a step that writes via TestContext".x(
                () => TestContext.Current.TestOutputHelper!.WriteLine("ambient output"));
    }
}
