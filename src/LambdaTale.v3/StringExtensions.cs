namespace LambdaTale.v3;

public static class StringExtensions
{
    public static void x(this string tale, Action lambda) => Scenario.Add(tale, lambda);

    public static void x(this string tale, Func<Task> lambda) => Scenario.Add(tale, lambda);
}
