using LambdaTale.v3.Tests.Harness;
using Xunit;

namespace LambdaTale.v3.Tests.Behavior;

public class BackgroundAndTeardownBehaviorTests
{
    [Fact]
    public async Task TeardownRunsAfterFailingMainStep()
    {
        var bus = await ScenarioTestRunner.RunFixture<TeardownRunsOnFailureFixture>(
            nameof(TeardownRunsOnFailureFixture.ScenarioWithFailingStep));

        var failed = bus.AssertStepFailed("Given a step that sets flag and fails");
        Assert.Contains("intentional failure", failed.Messages.Single());

        bus.AssertStepPassed("teardown runs and confirms");
    }

    [Fact]
    public async Task BackgroundFailureBeforeRegisteringStepsRunsTeardown()
    {
        var bus = await ScenarioTestRunner.RunFixture<BackgroundThrowsBeforeRegisteringFixture>(
            nameof(BackgroundThrowsBeforeRegisteringFixture.ScenarioStepsDoNotRun));

        var failed = bus.AssertStepFailed("(Background)");
        Assert.Contains("background failed before registering steps", failed.Messages.Single());

        bus.AssertStepPassed("teardown still runs after background failure");
    }

    [Fact]
    public async Task BackgroundFailureAfterRegisteringStepsRunsTeardown()
    {
        var bus = await ScenarioTestRunner.RunFixture<BackgroundThrowsAfterRegisteringFixture>(
            nameof(BackgroundThrowsAfterRegisteringFixture.ScenarioStepsDoNotRun));

        var failed = bus.AssertStepFailed("(Background)");
        Assert.Contains("background failed after registering a step", failed.Messages.Single());

        bus.AssertStepPassed("teardown still runs after background failure");
    }

    [Fact]
    public async Task MultipleBackgroundMethodsCauseConfigurationError()
    {
        var bus = await ScenarioTestRunner.RunFixture<MultipleBackgroundsFixture>(
            nameof(MultipleBackgroundsFixture.ScenarioDoesNotRun));

        var failed = bus.AssertStepFailed("(Configuration Error)");
        Assert.Contains("Multiple [BackgroundAttribute] methods found", failed.Messages.Single());
    }

    [Fact]
    public async Task MultipleTeardownMethodsCauseConfigurationError()
    {
        var bus = await ScenarioTestRunner.RunFixture<MultipleTeardownsFixture>(
            nameof(MultipleTeardownsFixture.ScenarioDoesNotRun));

        var failed = bus.AssertStepFailed("(Configuration Error)");
        Assert.Contains("Multiple [TeardownAttribute] methods found", failed.Messages.Single());
    }

    [Fact]
    public async Task TeardownMethodThrowSurfacesAsSyntheticFailure()
    {
        var bus = await ScenarioTestRunner.RunFixture<TeardownMethodThrowsFixture>(
            nameof(TeardownMethodThrowsFixture.ScenarioPassesButTeardownFails));

        bus.AssertStepPassed("Given a passing scenario step");

        var failed = bus.AssertStepFailed("(Teardown)");
        Assert.Contains("teardown method threw before registering steps", failed.Messages.Single());
    }

    private sealed class TeardownRunsOnFailureFixture
    {
        private bool mainStepReached;

        [Teardown]
        public void Cleanup() =>
            "Then teardown runs and confirms main step was reached".x(()
                => Assert.True(this.mainStepReached));

        public void ScenarioWithFailingStep() =>
            "Given a step that sets flag and fails".x(() =>
            {
                this.mainStepReached = true;
                throw new InvalidOperationException("intentional failure");
            });
    }

    private sealed class BackgroundThrowsBeforeRegisteringFixture
    {
        [Background]
        public void Setup() =>
            throw new InvalidOperationException("background failed before registering steps");

        [Teardown]
        public void Cleanup() =>
            "Then teardown still runs after background failure".x(() => { });

        public void ScenarioStepsDoNotRun() =>
            "Then this step should not execute".x(()
                => throw new InvalidOperationException("should not run"));
    }

    private sealed class BackgroundThrowsAfterRegisteringFixture
    {
        [Background]
        public void Setup()
        {
            "Given background step that was registered".x(() => { });
            throw new InvalidOperationException("background failed after registering a step");
        }

        [Teardown]
        public void Cleanup() =>
            "Then teardown still runs after background failure".x(() => { });

        public void ScenarioStepsDoNotRun() =>
            "Then this step should not execute".x(()
                => throw new InvalidOperationException("should not run"));
    }

    private sealed class MultipleBackgroundsFixture
    {
        [Background]
        public void Setup1() =>
            "Given background 1".x(() => throw new InvalidOperationException("should not run"));

        [Background]
        public void Setup2() =>
            "Given background 2".x(() => throw new InvalidOperationException("should not run"));

        public void ScenarioDoesNotRun() =>
            "Then this step should not execute".x(()
                => throw new InvalidOperationException("should not run"));
    }

    private sealed class MultipleTeardownsFixture
    {
        [Teardown]
        public void Cleanup1() =>
            "Then teardown 1".x(() => throw new InvalidOperationException("should not run"));

        [Teardown]
        public void Cleanup2() =>
            "Then teardown 2".x(() => throw new InvalidOperationException("should not run"));

        public void ScenarioDoesNotRun() =>
            "Then this step should not execute".x(()
                => throw new InvalidOperationException("should not run"));
    }

    private sealed class TeardownMethodThrowsFixture
    {
        [Teardown]
        public void Cleanup() =>
            throw new InvalidOperationException("teardown method threw before registering steps");

        public void ScenarioPassesButTeardownFails() =>
            "Given a passing scenario step".x(() => { });
    }
}
