using System.Diagnostics.CodeAnalysis;

namespace LambdaTale.v3;

public sealed class Scenario
{
    private static readonly AsyncLocal<List<ScenarioTestDefinition>?> Tests = new();
    private static readonly AsyncLocal<int> LambdaIndex = new();

    public static IDisposable Acquire()
    {
        Tests.Value = [];
        LambdaIndex.Value = 0;
        return ScenarioContext.Instance;
    }

    public static void Add(string tale, Action lambda)
    {
        var context = Tests.Value ?? MissingContext();
        context.Add(new ScenarioTestDefinition(tale, lambda, LambdaIndex.Value));
        LambdaIndex.Value++;
    }

    public static IEnumerable<ScenarioTestDefinition> TestDefinitions =>
        Tests.Value ?? MissingContext();

    [DoesNotReturn]
    private static List<ScenarioTestDefinition> MissingContext() => throw new InvalidOperationException("Missing " + nameof(ScenarioContext));

    private sealed class ScenarioContext : IDisposable
    {
        public static readonly IDisposable Instance = new ScenarioContext();

        public void Dispose() => Tests.Value = null;
    }
}

public record ScenarioTestDefinition(string Tale, Action Lambda, int index);
