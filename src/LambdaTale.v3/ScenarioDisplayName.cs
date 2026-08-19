using System.Reflection;
using Xunit.v3;

namespace LambdaTale.v3;

internal static class ScenarioDisplayName
{
    public static string ForTestCase(
        IXunitTestMethod testMethod,
        string baseDisplayName,
        string? label,
        object?[]? arguments)
    {
        var displayName = testMethod.GetDisplayName(baseDisplayName, label, arguments, methodGenericTypes: null);

        return arguments is null
            ? displayName
            : TrimUnsuppliedParameters(displayName, testMethod.Parameters.ToArray(), arguments.Length);
    }

    private static string TrimUnsuppliedParameters(string displayName, ParameterInfo[] parameters, int suppliedCount)
    {
        for (var i = parameters.Length - 1; i >= suppliedCount; i--)
        {
            var placeholder = $"{parameters[i].Name}: ???";
            if (displayName.EndsWith($"({placeholder})", StringComparison.Ordinal))
            {
                displayName = displayName[..^(placeholder.Length + 2)];
            }
            else if (displayName.EndsWith($", {placeholder})", StringComparison.Ordinal))
            {
                displayName = displayName[..^(placeholder.Length + 3)] + ")";
            }
            else
            {
                break;
            }
        }

        return displayName;
    }
}
