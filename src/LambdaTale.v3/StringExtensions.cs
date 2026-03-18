namespace LambdaTale.v3;

public static class StringExtensions
{
    public static void x(this string tale, Action lambda, OnError onError = OnError.Stop) =>
        Scenario.Add(tale, lambda, onError);

    public static void x(this string tale, Func<Task> lambda, OnError onError = OnError.Stop) =>
        Scenario.Add(tale, lambda, onError);

    public static void ContinueOnError(this string tale, Action lambda) =>
        Scenario.Add(tale, lambda, OnError.Continue);

    public static void ContinueOnError(this string tale, Func<Task> lambda) =>
        Scenario.Add(tale, lambda, OnError.Continue);

    public static void StopOnError(this string tale, Action lambda) =>
        Scenario.Add(tale, lambda, OnError.Stop);

    public static void StopOnError(this string tale, Func<Task> lambda) =>
        Scenario.Add(tale, lambda, OnError.Stop);
}
