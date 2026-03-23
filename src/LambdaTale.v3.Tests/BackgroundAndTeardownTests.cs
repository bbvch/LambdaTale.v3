using Xunit;

namespace LambdaTale.v3.Tests;

// ─── Background ───────────────────────────────────────────────────────────────

/// Tests that background steps run before scenario steps.
public class BackgroundOrderTests
{
    private readonly List<string> _log = [];

    [Background]
    public void Setup()
    {
        "Given background setup".x(() => _log.Add("background"));
    }

    [Scenario]
    public void BackgroundRunsBeforeScenario()
    {
        "When the scenario runs".x(() => _log.Add("scenario"));
        "Then background ran first".x(() => Assert.Equal(["background", "scenario"], _log));
    }
}

/// Tests that background steps run before scenario steps when scenario has multiple steps.
public class BackgroundWithMultipleScenarioStepsTests
{
    private readonly List<string> _log = [];

    [Background]
    public void Setup()
    {
        "Given background step 1".x(() => _log.Add("bg1"));
        "And background step 2".x(() => _log.Add("bg2"));
    }

    [Scenario]
    public void BackgroundStepsPrecedeScenarioSteps()
    {
        "When step A".x(() => _log.Add("a"));
        "And step B".x(() => _log.Add("b"));
        "Then order is correct".x(() => Assert.Equal(["bg1", "bg2", "a", "b"], _log));
    }
}

// ─── Teardown ─────────────────────────────────────────────────────────────────

/// Tests that teardown steps run after scenario steps.
public class TeardownOrderTests
{
    private readonly List<string> _log = [];

    [Teardown]
    public void Cleanup()
    {
        "Then teardown cleanup".x(() =>
        {
            _log.Add("teardown");
            Assert.Equal(["scenario", "teardown"], _log);
        });
    }

    [Scenario]
    public void TeardownRunsAfterScenario()
    {
        "Given the scenario runs".x(() => _log.Add("scenario"));
        "And teardown has not yet run".x(() => Assert.DoesNotContain("teardown", _log));
    }
}

/// Tests that async background and async teardown methods are awaited correctly.
public class AsyncBackgroundAndTeardownTests
{
    private readonly List<string> _log = [];

    [Background]
    public async Task AsyncSetup()
    {
        await Task.Yield();
        "Given async background".x(() => _log.Add("bg"));
    }

    [Teardown]
    public async Task AsyncCleanup()
    {
        await Task.Yield();
        "Then async teardown".x(() =>
        {
            _log.Add("td");
            Assert.Equal(["bg", "scenario", "td"], _log);
        });
    }

    [Scenario]
    public void AsyncMethodsAreAwaited()
    {
        "When scenario runs".x(() => _log.Add("scenario"));
        "Then background ran before scenario".x(() => Assert.Equal(["bg", "scenario"], _log));
    }
}

/// Tests that teardown runs even when a scenario step fails.
/// The main step fails intentionally; the teardown step asserts it still ran.
/// NOTE: The step "[0] Given a step that sets flag and fails" will appear as FAILED
/// in test output — this is intentional and demonstrates the feature.
public class TeardownRunsOnFailureTests
{
    private bool _mainStepReached;

    [Teardown]
    public void Cleanup()
    {
        "Then teardown runs and confirms main step was reached".x(()
            => Assert.True(_mainStepReached));
    }

    [Scenario]
    public void ScenarioWithFailingStep()
    {
        "Given a step that sets flag and fails".x(() =>
        {
            _mainStepReached = true;
            throw new InvalidOperationException("intentional failure");
        });
    }
}

// ─── Configuration Errors ─────────────────────────────────────────────────────

/// Tests that multiple [Background] methods result in a configuration error and no steps run.
/// NOTE: The "(Configuration Error)" synthetic failure and no scenario steps appear in output.
public class MultipleBackgroundMethodsTests
{
    [Background]
    public void Setup1()
    {
        "Given background 1".x(() => throw new InvalidOperationException("should not run"));
    }

    [Background]
    public void Setup2()
    {
        "Given background 2".x(() => throw new InvalidOperationException("should not run"));
    }

    [Scenario]
    public void ScenarioDoesNotRun()
    {
        "Then this step should not execute".x(() => throw new InvalidOperationException("should not run"));
    }
}

/// Tests that multiple [Teardown] methods result in a configuration error and no steps run.
/// NOTE: The "(Configuration Error)" synthetic failure and no scenario steps appear in output.
public class MultipleTeardownMethodsTests
{
    [Teardown]
    public void Cleanup1()
    {
        "Then teardown 1".x(() => throw new InvalidOperationException("should not run"));
    }

    [Teardown]
    public void Cleanup2()
    {
        "Then teardown 2".x(() => throw new InvalidOperationException("should not run"));
    }

    [Scenario]
    public void ScenarioDoesNotRun()
    {
        "Then this step should not execute".x(() => throw new InvalidOperationException("should not run"));
    }
}

// ─── Background Failure ───────────────────────────────────────────────────────

/// Tests that when background throws before registering steps, a failure is reported
/// and teardown still runs. The "(Background)" synthetic failure appears in output.
public class BackgroundThrowsBeforeRegisteringStepsTests
{
    private bool _teardownRan;

    [Background]
    public void Setup()
    {
        throw new InvalidOperationException("background failed before registering steps");
    }

    [Teardown]
    public void Cleanup()
    {
        "Then teardown still runs after background failure".x(() =>
        {
            _teardownRan = true;
            Assert.True(_teardownRan);
        });
    }

    [Scenario]
    public void ScenarioStepsDoNotRun()
    {
        "Then this step should not execute".x(() => throw new InvalidOperationException("should not run"));
    }
}

/// Tests that when background throws after registering some steps, those steps are not
/// executed, a failure is reported, and teardown still runs.
public class BackgroundThrowsAfterRegisteringStepsTests
{
    private bool _teardownRan;

    [Background]
    public void Setup()
    {
        "Given background step that was registered".x(() => { });
        throw new InvalidOperationException("background failed after registering a step");
    }

    [Teardown]
    public void Cleanup()
    {
        "Then teardown still runs after background failure".x(() =>
        {
            _teardownRan = true;
            Assert.True(_teardownRan);
        });
    }

    [Scenario]
    public void ScenarioStepsDoNotRun()
    {
        "Then this step should not execute".x(() => throw new InvalidOperationException("should not run"));
    }
}

// ─── Teardown Failure ─────────────────────────────────────────────────────────

/// Tests that when the teardown method itself throws, a "(Teardown)" synthetic failure
/// is reported. NOTE: The "(Teardown)" failure appears in output — this is intentional.
public class TeardownMethodThrowsTests
{
    [Teardown]
    public void Cleanup()
    {
        throw new InvalidOperationException("teardown method threw before registering steps");
    }

    [Scenario]
    public void ScenarioPassesButTeardownFails()
    {
        "Given a passing scenario step".x(() => { });
    }
}
