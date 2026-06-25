using System.Collections.Concurrent;
using System.Reflection;

namespace LambdaTale.v3;

internal readonly record struct FixtureMethods(MethodInfo? Background, MethodInfo? Teardown, string? ConfigError);

// [Background]/[Teardown] resolution depends only on the test class type, so it is cached
// once per type rather than re-scanned on every test case and every delay-enumerated row.
internal static class FixtureMethodResolver
{
    private static readonly ConcurrentDictionary<Type, FixtureMethods> Cache = new();

    public static FixtureMethods Resolve(Type testClass) =>
        Cache.GetOrAdd(testClass, static type =>
        {
            MethodInfo? background = null;
            MethodInfo? teardown = null;
            var backgroundCount = 0;
            var teardownCount = 0;

            foreach (var method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<BackgroundAttribute>() is not null)
                {
                    background = method;
                    backgroundCount++;
                }

                if (method.GetCustomAttribute<TeardownAttribute>() is not null)
                {
                    teardown = method;
                    teardownCount++;
                }
            }

            string? configError = null;
            if (backgroundCount > 1 || teardownCount > 1)
            {
                var offenders = new List<string>();
                if (backgroundCount > 1)
                {
                    offenders.Add(nameof(BackgroundAttribute));
                }

                if (teardownCount > 1)
                {
                    offenders.Add(nameof(TeardownAttribute));
                }

                var which = string.Join(" and ", offenders.Select(o => $"[{o}]"));
                configError = $"Multiple {which} methods found. Only one is allowed per class.";
            }

            return new FixtureMethods(background, teardown, configError);
        });
}
