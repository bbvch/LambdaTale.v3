namespace LambdaTale.v3;

public static class StringExtensions
{
    public static void x(this string tale, Action lambda) => Scenario.Add(tale, lambda);
}
