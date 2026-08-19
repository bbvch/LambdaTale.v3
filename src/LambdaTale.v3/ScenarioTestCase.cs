using System.Diagnostics;
using System.Reflection;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

[DebuggerDisplay(@"\{ class = {TestMethod.TestClass.TestClassName}, method = {TestMethod.MethodName}, display = {TestCaseDisplayName} \}")]
public sealed class ScenarioTestCase : ISelfExecutingXunitTestCase, IXunitDelayEnumeratedTestCase, IXunitSerializable
{
    private string? testCaseDisplayName;
    private bool isDelayEnumerated;
    private bool skipTestWithoutData;
    private bool isExplicit;

    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public ScenarioTestCase() { }

    public ScenarioTestCase(
        IXunitTestMethod testMethod,
        object?[]? testMethodArguments = null,
        string? testCaseDisplayName = null,
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
    {
        this.TestMethod = Guard.ArgumentNotNull(testMethod);
        this.TestMethodArguments = testMethodArguments;
        this.testCaseDisplayName = testCaseDisplayName;
        this.SkipReason = skipReason;
        this.SourceFilePath = sourceFilePath;
        this.SourceLineNumber = sourceLineNumber;
        this.isDelayEnumerated = isDelayEnumerated;
        this.skipTestWithoutData = skipTestWithoutData;
        this.isExplicit = isExplicit;
        this.SkipExceptions = skipExceptions;
        this.SkipType = skipType;
        this.SkipUnless = skipUnless;
        this.SkipWhen = skipWhen;
        this.Timeout = timeout;
        this.DisableParallelization = disableParallelization;
    }

    public IXunitTestMethod TestMethod
    {
        get =>
            field ?? throw new InvalidOperationException($"Uninitialized {nameof(ScenarioTestCase)}.{nameof(this.TestMethod)}");
        private set;
    }

    public object?[]? TestMethodArguments { get; private set; }

    // Serialize an argument to a stable string for UniqueID generation. Falls back to a
    // type-qualified ToString() when xUnit's serializer doesn't support the type, rather
    // than throwing and preventing test discovery.
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

    public string UniqueID
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            using var g = new UniqueIDGenerator();
            g.Add(this.TestMethod.UniqueID);
            if (this.TestMethodArguments is not null)
            {
                foreach (var arg in this.TestMethodArguments)
                {
                    g.Add(SerializeArgForId(arg));
                }
            }

            if (this.isDelayEnumerated)
            {
                g.Add("delayed");
            }

            return field = g.Compute();
        }
    }

    public string TestCaseDisplayName =>
        this.testCaseDisplayName ?? this.TestMethod.GetDisplayName(this.TestMethod.MethodName, null, this.TestMethodArguments, null);

    public IXunitTestClass TestClass => this.TestMethod.TestClass;
    public IXunitTestCollection TestCollection => this.TestMethod.TestClass.TestCollection;

    public int TestClassMetadataToken => this.TestMethod.TestClass.Class.MetadataToken;
    public string TestClassName => this.TestMethod.TestClass.TestClassName;
    public string TestClassSimpleName => this.TestMethod.TestClass.TestClassSimpleName;

    public int TestMethodMetadataToken => this.TestMethod.Method.MetadataToken;
    public string TestMethodName => this.TestMethod.MethodName;

    public string[] TestMethodParameterTypesVSTest =>
        field ??=
            [.. this.TestMethod.Parameters.Select(p => p.ParameterType.FullName ?? p.ParameterType.Name)];

    public string TestMethodReturnTypeVSTest => this.TestMethod.ReturnType.FullName ?? this.TestMethod.ReturnType.Name;

    public int TestMethodArity =>
        this.TestMethod.Method.IsGenericMethodDefinition
            ? this.TestMethod.Method.GetGenericArguments().Length
            : 0;

    public string? SkipReason { get; private set; }
    public Type? SkipType { get; private set; }

    public string? SkipUnless { get; private set; }

    public string? SkipWhen { get; private set; }

    public Type[]? SkipExceptions { get; private set; }

    public int Timeout { get; private set; }

    public bool DisableParallelization { get; private set; }

    public string? SourceFilePath { get; private set; }
    public int? SourceLineNumber { get; private set; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.TestMethod.Traits;

    public ValueTask<IReadOnlyCollection<IXunitTest>> CreateTests() => new([]);
    public void PreInvoke() { }
    public void PostInvoke() { }

    ITestClass ITestCase.TestClass => this.TestClass;
    ITestCollection ITestCase.TestCollection => this.TestCollection;
    ITestMethod ITestCase.TestMethod => this.TestMethod;
    ICoreTestClass ICoreTestCase.TestClass => this.TestClass;
    ICoreTestCollection ICoreTestCase.TestCollection => this.TestCollection;
    ICoreTestMethod ICoreTestCase.TestMethod => this.TestMethod;
    bool ITestCaseMetadata.Explicit => this.isExplicit;
    string? ITestCaseMetadata.SkipReason => this.SkipReason;
    int? ITestCaseMetadata.TestClassMetadataToken => this.TestClassMetadataToken;
    string? ITestCaseMetadata.TestClassNamespace => this.TestMethod.TestClass.Class.Namespace;
    int? ITestCaseMetadata.TestMethodArity => this.MethodArityOrNull;
    int? ITestCaseMetadata.TestMethodMetadataToken => this.TestMethodMetadataToken;
    string[] ITestCaseMetadata.TestMethodParameterTypesVSTest => this.TestMethodParameterTypesVSTest;
    string ITestCaseMetadata.TestMethodReturnTypeVSTest => this.TestMethodReturnTypeVSTest;

    bool IXunitDelayEnumeratedTestCase.SkipTestWithoutData => this.skipTestWithoutData;

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("tm", this.TestMethod);
        info.AddValue("dn", this.testCaseDisplayName);
        info.AddValue("sr", this.SkipReason);
        info.AddValue("sf", this.SourceFilePath);
        info.AddValue("sl", this.SourceLineNumber);
        info.AddValue("de", this.isDelayEnumerated);
        info.AddValue("swd", this.skipTestWithoutData);
        info.AddValue("ex", this.isExplicit);
        var skipExc = this.SkipExceptions?.Select(t => t.AssemblyQualifiedName ?? t.FullName ?? t.Name).ToArray();
        info.AddValue("sx", skipExc);
        info.AddValue("st", this.SkipType?.AssemblyQualifiedName ?? this.SkipType?.FullName);
        info.AddValue("su", this.SkipUnless);
        info.AddValue("sw", this.SkipWhen);
        info.AddValue("to", this.Timeout);
        info.AddValue("dp", this.DisableParallelization);
        var argc = this.TestMethodArguments?.Length ?? -1;
        info.AddValue("argc", argc);
        if (this.TestMethodArguments is not null)
        {
            for (var i = 0; i < this.TestMethodArguments.Length; i++)
            {
                info.AddValue($"arg{i}", SerializationHelper.Instance.Serialize(this.TestMethodArguments[i]));
            }
        }
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        this.TestMethod = Guard.NotNull("Could not retrieve TestMethod from serialization", info.GetValue<IXunitTestMethod>("tm"));
        this.testCaseDisplayName = info.GetValue<string?>("dn");
        this.SkipReason = info.GetValue<string?>("sr");
        this.SourceFilePath = info.GetValue<string?>("sf");
        this.SourceLineNumber = info.GetValue<int?>("sl");
        this.isDelayEnumerated = info.GetValue<bool>("de");
        this.skipTestWithoutData = info.GetValue<bool>("swd");
        this.isExplicit = info.GetValue<bool>("ex");
        var skipExc = info.GetValue<string[]?>("sx");
        this.SkipExceptions = skipExc?.Select(name => Type.GetType(name, throwOnError: true)!).ToArray();
        var skipTypeName = info.GetValue<string?>("st");
        this.SkipType = skipTypeName is null ? null : Type.GetType(skipTypeName, throwOnError: true);
        this.SkipUnless = info.GetValue<string?>("su");
        this.SkipWhen = info.GetValue<string?>("sw");
        this.Timeout = info.GetValue<int>("to");
        this.DisableParallelization = info.GetValue<bool>("dp");
        var argc = info.GetValue<int>("argc");
        if (argc >= 0)
        {
            this.TestMethodArguments = new object?[argc];
            for (var i = 0; i < argc; i++)
            {
                this.TestMethodArguments[i] = SerializationHelper.Instance.Deserialize(info.GetValue<string>($"arg{i}")!);
            }
        }
    }

    // A scenario's steps are an ordered narrative over shared state, so they always run
    // sequentially on this flow and parallelMode/scheduler are deliberately not consulted.
    // Failures are reported per step through the message bus rather than via the aggregator,
    // and method fixtures are not injected into steps.
    public async ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        ParallelMode parallelMode,
        ExecutionScheduler scheduler,
        FixtureMappingManager methodFixtureMappings)
    {
        var ids = new MsgIds(
            this.TestCollection.TestAssembly.UniqueID,
            this.TestCollection.UniqueID,
            this.TestClass.UniqueID,
            this.TestMethod.UniqueID,
            this.UniqueID);
        var emitter = new ScenarioMessageEmitter(
            messageBus, cancellationTokenSource, ids, this.isExplicit, this.Timeout, this.Traits);
        var startTime = DateTimeOffset.UtcNow;
        RunSummary summary;

        var explicitSkipReason = (@explicit: this.isExplicit, explicitOption) switch
        {
            (true, ExplicitOption.Off) => "Test is marked Explicit and was not selected to run",
            (false, ExplicitOption.Only) => "Only explicit tests were selected to run",
            _ => null,
        };
        var conditionalSkipReason = this.EvaluateConditionalSkip();
        var effectiveSkipReason = this.SkipReason ?? conditionalSkipReason ?? explicitSkipReason;

        await emitter.Queue(new TestCaseStarting
        {
            AssemblyUniqueID = ids.AssemblyId,
            Explicit = this.isExplicit,
            SkipReason = effectiveSkipReason,
            SourceFilePath = this.SourceFilePath,
            SourceLineNumber = this.SourceLineNumber,
            StartTime = startTime,
            TestCaseDisplayName = this.TestCaseDisplayName,
            TestCaseUniqueID = ids.CaseId,
            TestClassMetadataToken = this.TestClassMetadataToken,
            TestClassName = this.TestClassName,
            TestClassNamespace = this.TestMethod.TestClass.Class.Namespace,
            TestClassSimpleName = this.TestClassSimpleName,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestMethodArity = this.MethodArityOrNull,
            TestMethodMetadataToken = this.TestMethodMetadataToken,
            TestMethodName = this.TestMethodName,
            TestMethodParameterTypesVSTest = this.TestMethodParameterTypesVSTest,
            TestMethodReturnTypeVSTest = this.TestMethodReturnTypeVSTest,
            TestMethodUniqueID = ids.MethodId,
            Traits = this.Traits,
        });

        if (effectiveSkipReason is not null)
        {
            summary = await this.SendSkippedTestCase(emitter, effectiveSkipReason);
        }
        else
        {
            var dispatch = this.isDelayEnumerated
                ? ScenarioCaseRunner.RunDelayEnumerated(this, emitter, constructorArguments).AsTask()
                : ScenarioCaseRunner.RunWithArguments(this, emitter, constructorArguments, this.TestMethodArguments).AsTask();

            if (this.Timeout > 0)
            {
                var winner = await Task.WhenAny(dispatch, Task.Delay(this.Timeout));
                if (winner != dispatch)
                {
                    var elapsed = this.Timeout / 1000m;
                    var timeoutEx = new TimeoutException($"Test exceeded timeout of {this.Timeout}ms");
                    await emitter.ReportSyntheticFailure("(Timeout)", stepIndex: 0, timeoutEx, elapsed);
                    summary = new RunSummary { Total = 1, Failed = 1, Time = elapsed };
                }
                else
                {
                    summary = await dispatch;
                }
            }
            else
            {
                summary = await dispatch;
            }
        }

        await emitter.Queue(new TestCaseFinished
        {
            AssemblyUniqueID = ids.AssemblyId,
            ExecutionTime = summary.Time,
            FinishTime = DateTimeOffset.UtcNow,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestMethodUniqueID = ids.MethodId,
            TestsFailed = summary.Failed,
            TestsNotRun = 0,
            TestsSkipped = summary.Skipped,
            TestsTotal = summary.Total,
        });

        return summary;
    }

    private async ValueTask<RunSummary> SendSkippedTestCase(ScenarioMessageEmitter emitter, string skipReason)
    {
        await emitter.EmitSynthetic(
            emitter.TestUniqueId(0), this.TestCaseDisplayName, this.Traits, elapsed: 0m, new StepOutcome.Skipped(skipReason));
        return new RunSummary { Total = 1, Skipped = 1 };
    }

    private string? EvaluateConditionalSkip()
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

    // GetParameters() clones an array on each call; the parameters are constant per test method.
    internal ParameterInfo[] MethodParameters => field ??= this.TestMethod.Method.GetParameters();

    // ITestCaseMetadata reports arity as absent (rather than zero) for non-generic methods.
    private int? MethodArityOrNull => this.TestMethod.Method.IsGenericMethodDefinition ? this.TestMethodArity : null;
}
