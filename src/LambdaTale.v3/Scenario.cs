using System.Diagnostics.CodeAnalysis;

namespace LambdaTale.v3;

public static class Scenario
{
    private static readonly AsyncLocal<List<ScenarioTestDefinition>?> Tests = new();
    private static readonly AsyncLocal<int> LambdaIndex = new();

    public static IDisposable Acquire()
    {
        Tests.Value = [];
        LambdaIndex.Value = 0;
        return new Cleanup();
    }

    public static void Add(string tale, Action lambda, OnError onError = OnError.Stop)
    {
        var context = Tests.Value ?? MissingContext();
        context.Add(new ScenarioTestDefinition(tale, new TaleBody.SynchronousTaleBody(lambda), LambdaIndex.Value, onError));
        LambdaIndex.Value++;
    }

    public static void Add(string tale, Func<Task> body, OnError onError = OnError.Stop)
    {
        var context = Tests.Value ?? MissingContext();
        context.Add(new ScenarioTestDefinition(tale, new TaleBody.AsynchronousTaleBody(body), LambdaIndex.Value, onError));
        LambdaIndex.Value++;
    }

    public static IEnumerable<ScenarioTestDefinition> TestDefinitions =>
        Tests.Value ?? MissingContext();

    [DoesNotReturn]
    private static List<ScenarioTestDefinition> MissingContext() =>
        throw new InvalidOperationException($"Call {nameof(Scenario)}.{nameof(Acquire)}() before adding steps.");

    private sealed class Cleanup : IDisposable
    {
        public void Dispose() => Tests.Value = null;
    }
}

public record ScenarioTestDefinition(string Tale, TaleBody Lambda, int Index, OnError OnError = OnError.Stop);

public abstract record TaleBody
{
    public sealed record SynchronousTaleBody(Action Body) : TaleBody;

    public sealed record AsynchronousTaleBody(Func<Task> Body) : TaleBody;
}
