using Xunit;

namespace LambdaTale.v3.Tests.Integration;

public class BackgroundOrderTests
{
    private readonly List<string> log = [];

    [Background]
    public void Setup()
    {
        "Given background setup".x(() => this.log.Add("background"));
    }

    [Scenario]
    public void BackgroundRunsBeforeScenario()
    {
        "When the scenario runs".x(() => this.log.Add("scenario"));
        "Then background ran first".x(() => Assert.Equal(["background", "scenario"], this.log));
    }
}

public class BackgroundWithMultipleScenarioStepsTests
{
    private readonly List<string> log = [];

    [Background]
    public void Setup()
    {
        "Given background step 1".x(() => this.log.Add("bg1"));
        "And background step 2".x(() => this.log.Add("bg2"));
    }

    [Scenario]
    public void BackgroundStepsPrecedeScenarioSteps()
    {
        "When step A".x(() => this.log.Add("a"));
        "And step B".x(() => this.log.Add("b"));
        "Then order is correct".x(() => Assert.Equal(["bg1", "bg2", "a", "b"], this.log));
    }
}

public class TeardownOrderTests
{
    private readonly List<string> log = [];

    [Teardown]
    public void Cleanup()
    {
        "Then teardown cleanup".x(() =>
        {
            this.log.Add("teardown");
            Assert.Equal(["scenario", "teardown"], this.log);
        });
    }

    [Scenario]
    public void TeardownRunsAfterScenario()
    {
        "Given the scenario runs".x(() => this.log.Add("scenario"));
        "And teardown has not yet run".x(() => Assert.DoesNotContain("teardown", this.log));
    }
}

public class AsyncBackgroundAndTeardownTests
{
    private readonly List<string> log = [];

    [Background]
    public async Task AsyncSetup()
    {
        await Task.Yield();
        "Given async background".x(() => this.log.Add("bg"));
    }

    [Teardown]
    public async Task AsyncCleanup()
    {
        await Task.Yield();
        "Then async teardown".x(() =>
        {
            this.log.Add("td");
            Assert.Equal(["bg", "scenario", "td"], this.log);
        });
    }

    [Scenario]
    public void AsyncMethodsAreAwaited()
    {
        "When scenario runs".x(() => this.log.Add("scenario"));
        "Then background ran before scenario".x(() => Assert.Equal(["bg", "scenario"], this.log));
    }
}
