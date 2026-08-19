using System.Diagnostics;
using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

[DebuggerDisplay(@"\{ class = {TestMethod.TestClass.TestClassName}, method = {TestMethod.MethodName}, display = {TestCaseDisplayName} \}")]
public sealed class ScenarioTestCase : XunitTestCase, ISelfExecutingXunitTestCase, IXunitDelayEnumeratedTestCase
{
    private bool isDelayEnumerated;
    private bool skipTestWithoutData;

    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioTestCase() { }

    public ScenarioTestCase(
        IXunitTestMethod testMethod,
        object?[]? testMethodArguments = null,
        string? testCaseDisplayName = null,
        string? uniqueID = null,
        string? skipReason = null,
        string? sourceFilePath = null,
        int? sourceLineNumber = null,
        bool isDelayEnumerated = false,
        bool skipTestWithoutData = false,
        bool isExplicit = false,
        Type[]? skipExceptions = null,
        Type? skipType = null,
        string? skipUnless = null,
        string? skipWhen = null,
        int timeout = 0,
        bool disableParallelization = false)
        : base(
            testMethod,
            testCaseDisplayName ?? DefaultDisplayName(testMethod, testMethodArguments),
            uniqueID ?? ComputeUniqueID(testMethod, testMethodArguments),
            isExplicit,
            testLabel: null,
            disableParallelization,
            skipExceptions,
            skipReason,
            skipType,
            skipUnless,
            skipWhen,
            TestIntrospectionHelper.GetTraits(testMethod, dataRow: null),
            testMethodArguments,
            sourceFilePath,
            sourceLineNumber,
            timeout)
    {
        this.isDelayEnumerated = isDelayEnumerated;
        this.skipTestWithoutData = skipTestWithoutData;
    }

    // Steps are the reported tests, and they only exist once the scenario method has run. The
    // runner creates them as it goes, so there is nothing to enumerate up front.
    public override ValueTask<IReadOnlyCollection<IXunitTest>> CreateTests() => new([]);

    bool IXunitDelayEnumeratedTestCase.SkipTestWithoutData => this.skipTestWithoutData;

    internal bool IsDelayEnumerated => this.isDelayEnumerated;

    // A scenario's steps are an ordered narrative over shared state, so they always run
    // sequentially on this flow and parallelMode/scheduler are deliberately not consulted.
    // Method fixtures are not injected into steps.
    public ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        ParallelMode parallelMode,
        ExecutionScheduler scheduler,
        FixtureMappingManager methodFixtureMappings)
    {
        var explicitSkipReason = (@explicit: this.Explicit, explicitOption) switch
        {
            (true, ExplicitOption.Off) => "Test is marked Explicit and was not selected to run",
            (false, ExplicitOption.Only) => "Only explicit tests were selected to run",
            _ => null,
        };

        return ScenarioTestCaseRunner.RunCase(
            this,
            explicitOption,
            messageBus,
            aggregator,
            cancellationTokenSource,
            constructorArguments,
            this.SkipReason ?? this.EvaluateConditionalSkip() ?? explicitSkipReason);
    }

    protected override void Serialize(IXunitSerializationInfo info)
    {
        base.Serialize(info);
        info.AddValue("de", this.isDelayEnumerated);
        info.AddValue("swd", this.skipTestWithoutData);
    }

    protected override void Deserialize(IXunitSerializationInfo info)
    {
        base.Deserialize(info);
        this.isDelayEnumerated = info.GetValue<bool>("de");
        this.skipTestWithoutData = info.GetValue<bool>("swd");
    }

    internal string? EvaluateConditionalSkip()
    {
        if (this.SkipUnless is null && this.SkipWhen is null)
        {
            return null;
        }

        if (this.SkipUnless is not null && this.SkipWhen is not null)
        {
            throw new InvalidOperationException("Only one of SkipUnless or SkipWhen may be set.");
        }

        var propertyName = this.SkipUnless ?? this.SkipWhen!;
        var hostType = this.SkipType ?? this.TestClass.Class;
        var property = hostType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException(
                           $"Could not find public static property '{propertyName}' on type '{hostType.FullName}'.");
        var value = property.GetValue(obj: null);
        var truthy = value is true;

        var shouldSkip = this.SkipUnless is not null ? !truthy : truthy;
        if (!shouldSkip)
        {
            return null;
        }

        return this.SkipReason ?? $"Conditional skip ({propertyName})";
    }

    private static string DefaultDisplayName(IXunitTestMethod testMethod, object?[]? testMethodArguments) =>
        testMethod.GetDisplayName(testMethod.MethodName, label: null, testMethodArguments, methodGenericTypes: null);

    private static string ComputeUniqueID(IXunitTestMethod testMethod, object?[]? testMethodArguments)
    {
        try
        {
            return UniqueIDGenerator.ForTestCase(testMethod.UniqueID, testMethodGenericTypes: null, testMethodArguments);
        }
        catch (ArgumentException)
        {
            // ForTestCase serializes the arguments with xunit's serializer, which rejects types it
            // doesn't understand. A scenario's arguments are ordinary domain objects, so fall back
            // to hashing each one on its own terms rather than failing discovery.
            using var g = new UniqueIDGenerator();
            g.Add(testMethod.UniqueID);
            foreach (var arg in testMethodArguments!)
            {
                g.Add(SerializeArgForId(arg));
            }

            return g.Compute();
        }
    }

    // Falls back to a type-qualified ToString() when xUnit's serializer doesn't support the type,
    // rather than throwing and preventing test discovery.
    internal static string SerializeArgForId(object? arg)
    {
        try
        {
            return SerializationHelper.Instance.Serialize(arg);
        }
        catch (ArgumentException)
        {
            return arg is null ? ":null:" : $":{arg.GetType().AssemblyQualifiedName}:{arg}";
        }
    }
}
