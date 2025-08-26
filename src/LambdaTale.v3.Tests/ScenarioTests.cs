using Xunit;

namespace LambdaTale.v3.Tests;

public class ScenarioTests
{
    [Fact]
    public void ShouldFail() => Assert.True(false);

    [Fact]
    public void ShouldPass() => Assert.True(true);


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

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Theory(int value) => Assert.Equal(0, value);
}
