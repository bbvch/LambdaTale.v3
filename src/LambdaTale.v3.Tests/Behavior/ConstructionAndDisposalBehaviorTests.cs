using LambdaTale.v3.Tests.Harness;
using Xunit;

namespace LambdaTale.v3.Tests.Behavior;

public class ConstructionAndDisposalBehaviorTests
{
    [Fact]
    public async Task DisposeRunsAfterFailingMainStep()
    {
        var bus = await ScenarioTestRunner.RunFixture<DisposeRunsOnFailureFixture>(
            nameof(DisposeRunsOnFailureFixture.ScenarioWithFailingStep));

        var failed = bus.AssertStepFailed("Given a step that sets flag and fails");
        Assert.Contains("intentional failure", failed.Messages.Single());

        bus.AssertStepPassed("dispose runs and confirms");
    }

    [Fact]
    public async Task ConstructorFailureBeforeRegisteringStepsIsSyntheticFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<ConstructorThrowsBeforeRegisteringFixture>(
            nameof(ConstructorThrowsBeforeRegisteringFixture.ScenarioStepsDoNotRun));

        var failed = bus.AssertStepFailed("(Constructor)");
        Assert.Contains("constructor failed before registering steps", failed.Messages.Single());
    }

    [Fact]
    public async Task ConstructorFailureAfterRegisteringStepsIsSyntheticFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<ConstructorThrowsAfterRegisteringFixture>(
            nameof(ConstructorThrowsAfterRegisteringFixture.ScenarioStepsDoNotRun));

        var failed = bus.AssertStepFailed("(Constructor)");
        Assert.Contains("constructor failed after registering a step", failed.Messages.Single());
    }

    [Fact]
    public async Task DisposeMethodThrowSurfacesAsSyntheticFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<DisposeMethodThrowsFixture>(
            nameof(DisposeMethodThrowsFixture.ScenarioPassesButDisposeFails));

        bus.AssertStepPassed("Given a passing scenario step");

        var failed = bus.AssertStepFailed("(Dispose)");
        Assert.Contains("dispose method threw before registering steps", failed.Messages.Single());
    }

    [Fact]
    public async Task AsyncDisposeMethodIsAwaitedAndItsStepsRun()
    {
        var bus = await ScenarioTestRunner.RunFixture<AsyncDisposeFixture>(
            nameof(AsyncDisposeFixture.ScenarioPasses));

        bus.AssertStepPassed("Given a passing scenario step");
        bus.AssertStepPassed("Then async dispose step runs after scenario");
    }

    private sealed class DisposeRunsOnFailureFixture : IDisposable
    {
        private bool mainStepReached;

        public void Dispose() =>
            "Then dispose runs and confirms main step was reached".x(()
                => Assert.True(this.mainStepReached));

        public void ScenarioWithFailingStep() =>
            "Given a step that sets flag and fails".x(() =>
            {
                this.mainStepReached = true;
                throw new InvalidOperationException("intentional failure");
            });
    }

    private sealed class ConstructorThrowsBeforeRegisteringFixture
    {
        public ConstructorThrowsBeforeRegisteringFixture() =>
            throw new InvalidOperationException("constructor failed before registering steps");

        public void ScenarioStepsDoNotRun() =>
            "Then this step should not execute".x(()
                => throw new InvalidOperationException("should not run"));
    }

    private sealed class ConstructorThrowsAfterRegisteringFixture
    {
        public ConstructorThrowsAfterRegisteringFixture()
        {
            "Given constructor step that was registered".x(() => { });
            throw new InvalidOperationException("constructor failed after registering a step");
        }

        public void ScenarioStepsDoNotRun() =>
            "Then this step should not execute".x(()
                => throw new InvalidOperationException("should not run"));
    }

    private sealed class DisposeMethodThrowsFixture : IDisposable
    {
        public void Dispose() =>
            throw new InvalidOperationException("dispose method threw before registering steps");

        public void ScenarioPassesButDisposeFails() =>
            "Given a passing scenario step".x(() => { });
    }

    private sealed class AsyncDisposeFixture : IAsyncDisposable
    {
        private readonly List<string> log = [];

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            "Then async dispose step runs after scenario".x(() =>
            {
                this.log.Add("dispose");
                Assert.Equal(["scenario", "dispose"], this.log);
            });
        }

        public void ScenarioPasses() =>
            "Given a passing scenario step".x(() => this.log.Add("scenario"));
    }
}
