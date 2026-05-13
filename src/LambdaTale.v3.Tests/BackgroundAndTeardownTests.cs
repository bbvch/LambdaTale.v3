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

