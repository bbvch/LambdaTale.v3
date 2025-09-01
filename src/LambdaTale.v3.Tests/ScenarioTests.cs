using LambdaTale.v3.Framework;
using Xunit;

[assembly: TestFramework(typeof(CombinedTestFramework))]

namespace LambdaTale.v3.Tests;

public class ScenarioTests
{
    private readonly int classData = 10;

    [Fact]
    public void ShouldFail() => Assert.True(false);

    [Fact]
    public void ShouldPass() => Assert.True(true);


    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Theory(int value) => Assert.Equal(0, value);

    [Scenario]
    public void Scenario()
    {
        var x = 0;
        "Given a Tale setting a initial value of 1".x(()
            => x = 1);

        "When a Tale to increment the value is executed".x(()
            => x += 1);

        "Then the value has changed".x(()
            => Assert.Equal(2, x));
    }

    [Scenario]
    public void ScenarioUsingClassData()
    {
        var x = 0;
        "Given a Tale setting a initial value of 1".x(()
            => x = 1);

        "When a Tale to increment the value using data from a class member is invoked".x(()
            => x += this.classData);

        "Then the value has changed".x(()
            => Assert.Equal(11, x));
    }

    [Scenario]
    public void ScenarioWithAsyncLambda()
    {
        var x = 0;
        "Given a async tale setting the value".x(async () =>
        {
            x = 1;
            await Task.CompletedTask;
        });

        "When a Tale to increment the value using data from a class member is invoked".x(async () =>
        {
            await Task.Delay(10);
            x += 1;
        });

        "Then the value has changed".x(()
            => Assert.Equal(2, x));
    }

    // TODO: db: This can currently not be parsed as a testcase due to parameter count mismatch. We'll probably want this form though
    [Scenario]
    public void ScenarioWithVariablesDefinedAsParameters(int x)
    {
        "Given a Tale setting a initial value of 1".x(()
            => x = 1);

        "When a Tale to increment the value using data from a class member is invoked".x(()
            => x += 1);

        "Then the value has changed".x(()
            => Assert.Equal(2, x));
    }

    [Scenario]
    public void ScenarioWithInitializedContextOutsideOfSteps()
    {
        // NOTE: This should really not happen, as it doesn't make sense to write code outside of step definitions.
        // Code written before the first step should be able to be reasonably handled, though.
        // Any code in between, or after all, steps is undefined behavior.
        var x = 8;
        $"Given a value was initialized outside of the steps with value [{x}]".x(() => { });

        "When a Tale to increment the value is executed".x(()
            => x += 1);

        "Then the value has changed".x(()
            => Assert.Equal(9, x));
    }
}
