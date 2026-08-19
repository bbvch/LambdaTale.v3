# LambdaTale.v3

> BDD-style, natural-language test steps for [xUnit.net v3](https://github.com/xunit/xunit) —
> a successor to [LambdaTale v2](https://github.com/bbvch/LambdaTale).

Describe each step of a test in plain language and attach the code that fulfils it. A
`[Scenario]` reads top to bottom as a small story, and each step shows up as its own
result in the test runner.

```csharp
[Scenario]
public void Adding_an_item_to_the_cart()
{
    var cart = default(Cart);

    "Given an empty cart".x(() => cart = new Cart());
    "When I add a book".x(() => cart.Add(new Book("xUnit in Action")));
    "Then the cart has one item".x(() => Assert.Single(cart.Items));
}
```

## Highlights

- Built on xUnit.net v3's own discovery/execution extensibility
- `[Scenario]` is just a `FactAttribute`, so scenarios coexist with ordinary `[Fact]`
  and `[Theory]` tests in the same class and assembly.
- Every `.x()` step is reported to the runner as an individual test.

## Getting started

Requires **.NET 10** and **xUnit.net v3** on the Microsoft.Testing.Platform runner.

```xml
<PackageReference Include="bbv.LambdaTale.v3" Version="0.0.1-alpha.1" />
```

Then write a scenario.

```csharp
using LambdaTale.v3;
using Xunit;

public class CalculatorTests
{
    [Scenario]
    public void Adding_two_numbers()
    {
        var calculator = default(Calculator);
        var result = 0;

        "Given a calculator".x(() => calculator = new Calculator());
        "When I add 2 and 3".x(() => result = calculator.Add(2, 3));
        "Then the result is 5".x(() => Assert.Equal(5, result));
    }

    [Fact] // ordinary xUnit tests work side by side
    public void Subtraction_works() => Assert.Equal(1, new Calculator().Subtract(3, 2));
}
```

## Writing scenarios

**Steps** run in declaration order; each is a description plus an `Action` or
`Func<Task>` body. **Shared state** is just closures over the method's locals:

```csharp
// set up before the first step
var x = 8;
"When it is incremented".x(() => x += 1);
"And awaited work runs".x(async () => await Task.Delay(10));
"Then it is 9".x(() => Assert.Equal(9, x));
```

**Parameters and data.** Scenario methods may take parameters, and `[InlineData]`,
`[MemberData]`, and `[ClassData]` work as on a `[Theory]` — each row produces a separate
scenario:

```csharp
[Scenario]
[InlineData(2, "two")]
[InlineData(3, "three")]
public void With_inline_data(int value, string name)
{
    $"Given value is {value}".x(() => { });
    "Then value is positive".x(() => Assert.True(value > 0));
}
```

**Setup and cleanup lifecycle.** Put shared setup steps in the test class constructor,
and cleanup steps in `IDisposable.Dispose()` or `IAsyncDisposable.DisposeAsync()`:

```csharp
public MyScenarioTests() => "Given a fresh log".x(() => log.Clear());

public void Dispose() => "Then the log is flushed".x(() => log.Add("flushed"));
```

**Error handling.** A failing step stops the scenario and remaining steps are reported
skipped (`OnError.Stop`, the default). Use `OnError.Continue` — or `.ContinueOnError()` /
`.StopOnError()` — to keep going after a failure.

**Skip, explicit, timeout.** `[Scenario]` inherits the usual `FactAttribute` knobs:

```csharp
[Scenario(Skip = "not implemented yet")]
[Scenario(SkipUnless = nameof(FeatureEnabled))]   // public static bool
[Scenario(Explicit = true)]                       // runs only when selected
[Scenario(Timeout = 2000)]                        // fails past 2s
```

Throwing a skip exception from inside a step marks that step skipped rather than failed.

**Output.** Inject `ITestOutputHelper` via the test class constructor; output is captured
per step.

## Migrating from LambdaTale v2

The `.x()` step syntax is unchanged, so `[Scenario]` methods carry over directly. The
work is in the plumbing:

1. Migrate the test project to xUnit.net v3 first (the bulk of the effort) — see xUnit's
   [v2 → v3 guide](https://xunit.net/docs/getting-started/v3/migration).
2. Replace the `bbv.LambdaTale` package with `bbv.LambdaTale.v3` and the namespace with
   `LambdaTale.v3`.
3. The static `Spec(...)` form from v2 is not available — use `"description".x(() => …)`.

## How it works

`[Scenario]` is a `FactAttribute` with a custom `IXunitTestCaseDiscoverer`. The discoverer
expands data attributes into one test case per row; at execution time each case builds the
test class, invokes the scenario to collect its `.x()` steps, runs each step as an
individual test, then runs `Dispose()` / `DisposeAsync()` and emits any cleanup steps.
Everything goes through xUnit.net v3's own extensibility interfaces, so no custom test
framework is involved.

## Build and test

```bash
dotnet build
dotnet test
```

## License

MIT ◎ LambdaTale.v3 contributors. See [LICENSE](https://github.com/bbvch/LambdaTale.v3/blob/main/LICENSE).
