using System.Collections;
using Xunit;

namespace LambdaTale.v3.Tests;

public class ScenarioTests
{
    public static IEnumerable<TheoryDataRow<int, string>> TestMemberData = [new(1, "one"), new(2, "two"), new(3, "three")];

    private readonly int classData = 10;

    [Fact]
    public void ShouldPass() => Assert.True(true);

    [Theory]
    [InlineData(0)]
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

    [Scenario]
    public void ScenarioWithVariablesDefinedAsParameters(int x, string testString)
    {
        "Given a Tale setting a initial value of 1".x(() =>
        {
            x = 1;
            testString = "LambdaTale, yay!";
        });

        "When a Tale to increment the value using data from a class member is invoked".x(()
            => x += 1);

        "Then the value has changed".x(()
            => Assert.Equal(2, x));

        "And the string contains a value".x(() => Assert.Contains("LambdaTale", testString));
    }

    [Scenario]
    public void ScenarioWithInitializedContextOutsideOfSteps()
    {
        var x = 8;
        $"Given a value was initialized outside of the steps with value [{x}]".x(() => { });

        "When a Tale to increment the value is executed".x(()
            => x += 1);

        "Then the value has changed".x(()
            => Assert.Equal(9, x));
    }

    [Scenario]
    [InlineData(5)]
    public void ScenarioWithInlineData(int value)
    {
        $"Given value is {value}".x(() => { });
        "Then value is what is the specified value of '5'".x(() => Assert.Equal(5, value));
    }

    [Scenario]
    [InlineData(5)]
    public void ScenarioWithInlineDataAndVariables(int value, string varAlpha, string varBeta)
    {
        $"Given value is {value}".x(() => { });
        "Then value is what is the specified value of '5'".x(() => Assert.Equal(5, value));
    }

    [Scenario]
    [InlineData(2, "two")]
    [InlineData(3, "three")]
    [InlineData(4, "four")]
    public void ScenarioWithMultipleInlineDataRows(int value, string name)
    {
        $"Given value is {value}".x(() => { });
        $"Then name is '{name}'".x(() => Assert.NotEmpty(name));
        "And value is positive".x(() => Assert.True(value > 0));
    }

    [Scenario]
    [MemberData(nameof(TestMemberData))]
    public void ScenarioWithMemberData(int value, string name)
    {
        $"Given value is {value}".x(() => { });
        $"Then name is '{name}'".x(() => Assert.NotEmpty(name));
        "And value is positive".x(() => Assert.True(value > 0));
    }

    [Scenario]
    [ClassData(typeof(TestClassData))]
    public void ScenarioWithClassData(int value, string name)
    {
        $"Given value is {value}".x(() => { });
        $"Then name is '{name}'".x(() => Assert.NotEmpty(name));
        "And value is positive".x(() => Assert.True(value > 0));
    }

    [Scenario(Skip = "demonstrating skip")]
    public void SkippedScenario()
    {
        "Given a step".x(() => { });
        "When another step".x(() => { });
        "Then a final step".x(() => Assert.Fail("should not run"));
    }

    [Scenario(Skip = "demonstrating skip with data")]
    [InlineData(1)]
    [InlineData(2)]
    public void SkippedScenarioWithData(int value)
    {
        $"Given value is {value}".x(() => { });
        "Then it should never run".x(() => Assert.Fail("should not run"));
    }

    [Scenario]
    public void ScenarioUsingContinueOnError()
    {
        var x = 0;
        "Given x is set to 1 using ContinueOnError".ContinueOnError(() => x = 1);
        "Then x is 1".x(() => Assert.Equal(1, x));
    }

    [Scenario]
    public void ScenarioUsingStopOnError()
    {
        var x = 0;
        "Given x is set to 1 using StopOnError".StopOnError(() => x = 1);
        "Then x is 1".x(() => Assert.Equal(1, x));
    }

    [Scenario]
    public void ScenarioUsingXWithOnErrorParam()
    {
        var x = 0;
        "Given x is set to 1".x(() => x = 1, OnError.Continue);
        "Then x is 1".x(() => Assert.Equal(1, x));
    }

    private class TestClassData : IEnumerable<TheoryDataRow>
    {
        public IEnumerator<TheoryDataRow> GetEnumerator()
        {
            yield return new(1, "one");
            yield return new(2, "two");
            yield return new(3, "three");
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
