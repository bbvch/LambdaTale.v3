using LambdaTale.v3.Tests.Harness;
using Xunit;
using Xunit.Sdk;

namespace LambdaTale.v3.Tests.Behavior;

public class AttributeBehaviorTests
{
    public class DisplayName
    {
        [Fact]
        public async Task DisplayNameOverridesGeneratedName()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                testCaseDisplayName: "My Custom Display Name");

            var starting = Assert.Single(bus.OfType<ITestCaseStarting>());
            Assert.Equal("My Custom Display Name", starting.TestCaseDisplayName);
        }
    }

    public class Explicit
    {
        [Fact]
        public async Task ExplicitTestSkippedWhenExplicitOptionIsOff()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                @explicit: true,
                explicitOption: ExplicitOption.Off);

            var skipped = Assert.Single(bus.OfType<ITestSkipped>());
            Assert.Contains("Explicit", skipped.Reason);
        }

        [Fact]
        public async Task NonExplicitTestSkippedWhenExplicitOptionIsOnly()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                @explicit: false,
                explicitOption: ExplicitOption.Only);

            var skipped = Assert.Single(bus.OfType<ITestSkipped>());
            Assert.Contains("explicit", skipped.Reason);
        }

        [Fact]
        public async Task ExplicitTestRunsWhenExplicitOptionIsOn()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                @explicit: true,
                explicitOption: ExplicitOption.On);

            bus.AssertStepPassed("a step");
        }
    }

    public class SkipExceptions
    {
        [Fact]
        public async Task SkipExceptionConvertsStepFailureToSkip()
        {
            var bus = await ScenarioTestRunner.RunFixture<SkipExceptionFixture>(
                nameof(SkipExceptionFixture.Scenario),
                skipExceptions: [typeof(SkipExceptionFixture.MySkipException)]);

            var skipped = bus.AssertStepSkipped("step that wants to be skipped");
            Assert.Equal("skipping at runtime", skipped.Reason);
        }

        [Fact]
        public async Task SkipExceptionDoesNotTriggerStopOnError()
        {
            var bus = await ScenarioTestRunner.RunFixture<SkipExceptionWithFollowingStepFixture>(
                nameof(SkipExceptionWithFollowingStepFixture.Scenario),
                skipExceptions: [typeof(SkipExceptionFixture.MySkipException)]);

            bus.AssertStepSkipped("first step gets skipped");
            bus.AssertStepPassed("second step still runs");
        }
    }

    public class SkipConditionally
    {
        [Fact]
        public async Task SkipUnlessSkipsWhenPropertyIsFalse()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                skipType: typeof(SkipFlags),
                skipUnless: nameof(SkipFlags.ShouldNotRun));

            var skipped = Assert.Single(bus.OfType<ITestSkipped>());
            Assert.Contains(nameof(SkipFlags.ShouldNotRun), skipped.Reason);
        }

        [Fact]
        public async Task SkipUnlessRunsWhenPropertyIsTrue()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                skipType: typeof(SkipFlags),
                skipUnless: nameof(SkipFlags.ShouldRun));

            bus.AssertStepPassed("a step");
        }

        [Fact]
        public async Task SkipWhenSkipsWhenPropertyIsTrue()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                skipType: typeof(SkipFlags),
                skipWhen: nameof(SkipFlags.ShouldRun));

            var skipped = Assert.Single(bus.OfType<ITestSkipped>());
            Assert.Contains(nameof(SkipFlags.ShouldRun), skipped.Reason);
        }

        [Fact]
        public async Task SkipUnlessResolvesPropertyOnTestClassWhenSkipTypeIsNull()
        {
            var bus = await ScenarioTestRunner.RunFixture<FixtureWithFlag>(
                nameof(FixtureWithFlag.Scenario),
                skipUnless: nameof(FixtureWithFlag.IsEnabled));

            var skipped = Assert.Single(bus.OfType<ITestSkipped>());
            Assert.Contains(nameof(FixtureWithFlag.IsEnabled), skipped.Reason);
        }

        [Fact]
        public async Task BothSkipUnlessAndSkipWhenThrowsInvalidOperation()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await ScenarioTestRunner.RunFixture<SimpleFixture>(
                    nameof(SimpleFixture.Scenario),
                    skipType: typeof(SkipFlags),
                    skipUnless: nameof(SkipFlags.ShouldRun),
                    skipWhen: nameof(SkipFlags.ShouldRun)));

            Assert.Contains("Only one", ex.Message);
        }

        [Fact]
        public async Task MissingConditionalSkipPropertyThrowsInvalidOperation()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await ScenarioTestRunner.RunFixture<SimpleFixture>(
                    nameof(SimpleFixture.Scenario),
                    skipType: typeof(SkipFlags),
                    skipUnless: "NonExistentProperty"));

            Assert.Contains("NonExistentProperty", ex.Message);
        }
    }

    public class SkipReason
    {
        [Fact]
        public async Task SkipReasonSkipsScenarioWithoutRunningSteps()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                skipReason: "demonstrating skip");

            var skipped = Assert.Single(bus.OfType<ITestSkipped>());
            Assert.Equal("demonstrating skip", skipped.Reason);
            Assert.Empty(bus.OfType<ITestPassed>());
            Assert.DoesNotContain(bus.OfType<ITestStarting>(), m => m.TestDisplayName.Contains("a step"));
        }

        [Fact]
        public async Task SkipReasonWinsOverExplicitOption()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                skipReason: "explicit user skip",
                @explicit: true,
                explicitOption: ExplicitOption.Off);

            var skipped = Assert.Single(bus.OfType<ITestSkipped>());
            Assert.Equal("explicit user skip", skipped.Reason);
        }

        [Fact]
        public async Task SkipReasonWinsOverConditionalSkip()
        {
            var bus = await ScenarioTestRunner.RunFixture<SimpleFixture>(
                nameof(SimpleFixture.Scenario),
                skipReason: "explicit user skip",
                skipType: typeof(SkipFlags),
                skipUnless: nameof(SkipFlags.ShouldNotRun));

            var skipped = Assert.Single(bus.OfType<ITestSkipped>());
            Assert.Equal("explicit user skip", skipped.Reason);
        }
    }

    public class Timeout
    {
        [Fact]
        public async Task TimeoutFailsLongRunningScenario()
        {
            var bus = await ScenarioTestRunner.RunFixture<LongRunningFixture>(
                nameof(LongRunningFixture.Scenario),
                timeout: 50);

            var failed = bus.AssertStepFailed("(Timeout)");
            Assert.Contains("exceeded timeout", failed.Messages.Single());
        }
    }

    private sealed class SimpleFixture
    {
        public void Scenario() =>
            "a step".x(() => { });
    }

    private sealed class SkipExceptionFixture
    {
        public sealed class MySkipException(string message) : Exception(message);

        public void Scenario() =>
            "a step that wants to be skipped".x(()
                => throw new MySkipException("skipping at runtime"));
    }

    private sealed class SkipExceptionWithFollowingStepFixture
    {
        public void Scenario()
        {
            "the first step gets skipped".StopOnError(()
                => throw new SkipExceptionFixture.MySkipException("skipping"));
            "the second step still runs".x(() => { });
        }
    }

    private static class SkipFlags
    {
        public static bool ShouldRun => true;
        public static bool ShouldNotRun => false;
    }

    private sealed class FixtureWithFlag
    {
        public static bool IsEnabled => false;
        public void Scenario() => "a step".x(() => { });
    }

    private sealed class LongRunningFixture
    {
        public void Scenario() =>
            "long-running step".x(async () => await Task.Delay(500));
    }
}
