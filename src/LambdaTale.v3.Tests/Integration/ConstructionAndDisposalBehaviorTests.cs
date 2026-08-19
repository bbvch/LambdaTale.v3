using Xunit;

namespace LambdaTale.v3.Tests.Integration;

public class ConstructorOrderTests
{
    private readonly List<string> log = [];

    public ConstructorOrderTests()
    {
        "Given constructor setup".x(() => this.log.Add("constructor"));
    }

    [Scenario]
    public void ConstructorStepsRunBeforeScenario()
    {
        "When the scenario runs".x(() => this.log.Add("scenario"));
        "Then constructor ran first".x(() => Assert.Equal(["constructor", "scenario"], this.log));
    }
}

public class ConstructorWithMultipleScenarioStepsTests
{
    private readonly List<string> log = [];

    public ConstructorWithMultipleScenarioStepsTests()
    {
        "Given constructor step 1".x(() => this.log.Add("c1"));
        "And constructor step 2".x(() => this.log.Add("c2"));
    }

    [Scenario]
    public void ConstructorStepsPrecedeScenarioSteps()
    {
        "When step A".x(() => this.log.Add("a"));
        "And step B".x(() => this.log.Add("b"));
        "Then order is correct".x(() => Assert.Equal(["c1", "c2", "a", "b"], this.log));
    }
}

public class DisposeOrderTests : IDisposable
{
    private readonly List<string> log = [];

    public void Dispose()
    {
        "Then dispose cleanup".x(() =>
        {
            this.log.Add("dispose");
            Assert.Equal(["scenario", "dispose"], this.log);
        });
    }

    [Scenario]
    public void TeardownRunsAfterScenario()
    {
        "Given the scenario runs".x(() => this.log.Add("scenario"));
        "And dispose has not yet run".x(() => Assert.DoesNotContain("dispose", this.log));
    }
}

public class AsyncDisposableTests : IAsyncDisposable
{
    private readonly List<string> log = [];

    public AsyncDisposableTests()
    {
        "Given sync constructor".x(() => this.log.Add("c"));
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        "Then async dispose".x(() =>
        {
            this.log.Add("dispose");
            Assert.Equal(["c", "scenario", "dispose"], this.log);
        });
    }

    [Scenario]
    public void AsyncMethodsAreAwaited()
    {
        "When scenario runs".x(() => this.log.Add("scenario"));
        "Then constructor steps ran before scenario".x(() => Assert.Equal(["c", "scenario"], this.log));
    }
}

public class AsyncLifetimeTests : IAsyncLifetime
{
    private readonly List<string> log = [];

    public async ValueTask InitializeAsync()
    {
        await Task.Yield();
        "Given async initialization".x(() => this.log.Add("init"));
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        this.log.Add("dispose");
    }

    [Scenario]
    public void InitializeAsyncRunsBeforeTheScenario()
    {
        "When the scenario runs".x(() => this.log.Add("scenario"));
        "Then initialization ran first".x(() => Assert.Equal(["init", "scenario"], this.log));
    }
}
