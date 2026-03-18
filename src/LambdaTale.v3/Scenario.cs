using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
        throw new InvalidOperationException("Missing " + nameof(ScenarioContext));

    private sealed class ScenarioContext : IDisposable
    {
        public static readonly IDisposable Instance = new ScenarioContext();

        public void Dispose() => Tests.Value = null;
    }
}

public record ScenarioTestDefinition(string Tale, TaleBody Lambda, int index, OnError OnError = OnError.Stop);

public abstract record TaleBody(MethodInfo Method)
{
    public sealed record SynchronousTaleBody(Action Body) : TaleBody(Body.Method);

    public sealed record AsynchronousTaleBody(Func<Task> Body) : TaleBody(Body.Method);
}
