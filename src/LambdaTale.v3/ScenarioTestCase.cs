using System.Collections.Concurrent;
using System.Collections.Frozen;
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
        int timeout = 0)
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

    public int? TestMethodArity =>
        this.TestMethod.Method.IsGenericMethodDefinition
            ? this.TestMethod.Method.GetGenericArguments().Length
            : null;

    public string? SkipReason { get; private set; }
    public Type? SkipType { get; private set; }

    public string? SkipUnless { get; private set; }

    public string? SkipWhen { get; private set; }

    public Type[]? SkipExceptions { get; private set; }

    public int Timeout { get; private set; }

    public string? SourceFilePath { get; private set; }
    public int? SourceLineNumber { get; private set; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.TestMethod.Traits;

    public ValueTask<IReadOnlyCollection<IXunitTest>> CreateTests() => new([]);
    public void PreInvoke() { }
    public void PostInvoke() { }

    ITestClass ITestCase.TestClass => this.TestClass;
    ITestCollection ITestCase.TestCollection => this.TestCollection;
    ITestMethod ITestCase.TestMethod => this.TestMethod;
    bool ITestCaseMetadata.Explicit => this.isExplicit;
    string? ITestCaseMetadata.SkipReason => this.SkipReason;
    int? ITestCaseMetadata.TestClassMetadataToken => this.TestClassMetadataToken;
    string? ITestCaseMetadata.TestClassNamespace => this.TestMethod.TestClass.Class.Namespace;
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

    public async ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator __,
        CancellationTokenSource cancellationTokenSource)
    {
        var ids = new MsgIds(
            this.TestCollection.TestAssembly.UniqueID,
            this.TestCollection.UniqueID,
            this.TestClass.UniqueID,
            this.TestMethod.UniqueID,
            this.UniqueID);
        RunSummary summary;

        var explicitSkipReason = (@explicit: this.isExplicit, explicitOption) switch
        {
            (true, ExplicitOption.Off) => "Test is marked Explicit and was not selected to run",
            (false, ExplicitOption.Only) => "Only explicit tests were selected to run",
            _ => null,
        };
        var conditionalSkipReason = this.EvaluateConditionalSkip();
        var effectiveSkipReason = this.SkipReason ?? conditionalSkipReason ?? explicitSkipReason;

        await QueueOrCancel(messageBus, new TestCaseStarting
        {
            AssemblyUniqueID = ids.AssemblyId,
            Explicit = this.isExplicit,
            SkipReason = effectiveSkipReason,
            SourceFilePath = this.SourceFilePath,
            SourceLineNumber = this.SourceLineNumber,
            TestCaseDisplayName = this.TestCaseDisplayName,
            TestCaseUniqueID = ids.CaseId,
            TestClassMetadataToken = this.TestClassMetadataToken,
            TestClassName = this.TestClassName,
            TestClassNamespace = this.TestMethod.TestClass.Class.Namespace,
            TestClassSimpleName = this.TestClassSimpleName,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestMethodArity = this.TestMethodArity,
            TestMethodMetadataToken = this.TestMethodMetadataToken,
            TestMethodName = this.TestMethodName,
            TestMethodParameterTypesVSTest = this.TestMethodParameterTypesVSTest,
            TestMethodReturnTypeVSTest = this.TestMethodReturnTypeVSTest,
            TestMethodUniqueID = ids.MethodId,
            Traits = this.Traits,
        }, cancellationTokenSource);

        if (effectiveSkipReason is not null)
        {
            summary = await this.SendSkippedTestCase(messageBus, cancellationTokenSource, ids, effectiveSkipReason);
        }
        else
        {
            var dispatch = this.isDelayEnumerated
                ? this.RunDelayEnumerated(messageBus, constructorArguments, cancellationTokenSource, ids).AsTask()
                : this.RunWithArguments(messageBus, constructorArguments, this.TestMethodArguments, cancellationTokenSource, ids).AsTask();

            if (this.Timeout > 0)
            {
                var winner = await Task.WhenAny(dispatch, Task.Delay(this.Timeout));
                if (winner != dispatch)
                {
                    var elapsed = this.Timeout / 1000m;
                    var timeoutEx = new TimeoutException($"Test exceeded timeout of {this.Timeout}ms");
                    await this.ReportSyntheticFailure(messageBus, cancellationTokenSource, ids, "(Timeout)", stepIndex: 0, timeoutEx, elapsed);
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

        await QueueOrCancel(messageBus, new TestCaseFinished
        {
            AssemblyUniqueID = ids.AssemblyId,
            ExecutionTime = summary.Time,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestMethodUniqueID = ids.MethodId,
            TestsFailed = summary.Failed,
            TestsNotRun = 0,
            TestsSkipped = summary.Skipped,
            TestsTotal = summary.Total,
        }, cancellationTokenSource);

        return summary;
    }

    private async ValueTask<RunSummary> SendSkippedTestCase(
        IMessageBus messageBus,
        CancellationTokenSource cts,
        MsgIds ids,
        string skipReason)
    {
        var testUniqueId = UniqueIDGenerator.ForTest(ids.CaseId, 0);
        await this.EmitSynthetic(messageBus, cts, ids, testUniqueId, this.TestCaseDisplayName, this.Traits, elapsed: 0m, new StepOutcome.Skipped(skipReason));
        return new RunSummary { Total = 1, Skipped = 1 };
    }

    private abstract record StepOutcome
    {
        public sealed record Passed : StepOutcome;

        public sealed record Skipped(string Reason) : StepOutcome;

        public sealed record Failed(Exception Exception) : StepOutcome;
    }

    private ValueTask EmitStarting(
        IMessageBus messageBus,
        CancellationTokenSource cts,
        MsgIds ids,
        string testUniqueId,
        string displayName,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> traits,
        DateTimeOffset startTime) =>
        QueueOrCancel(messageBus, new TestStarting
        {
            AssemblyUniqueID = ids.AssemblyId,
            Explicit = this.isExplicit,
            StartTime = startTime,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestDisplayName = displayName,
            TestMethodUniqueID = ids.MethodId,
            TestUniqueID = testUniqueId,
            Timeout = this.Timeout,
            Traits = traits,
        }, cts);

    private async ValueTask EmitOutcome(
        IMessageBus messageBus,
        CancellationTokenSource cts,
        MsgIds ids,
        string testUniqueId,
        DateTimeOffset finishTime,
        decimal elapsed,
        StepOutcome outcome)
    {
        TestFailed MakeFailed(Exception ex)
        {
            var (types, messages, stackTraces, indices, cause) = ExceptionUtility.ExtractMetadata(ex);
            return new TestFailed
            {
                AssemblyUniqueID = ids.AssemblyId,
                Cause = cause,
                ExceptionParentIndices = indices,
                ExceptionTypes = types,
                ExecutionTime = elapsed,
                FinishTime = finishTime,
                Messages = messages,
                Output = string.Empty,
                StackTraces = stackTraces,
                TestCaseUniqueID = ids.CaseId,
                TestClassUniqueID = ids.ClassId,
                TestCollectionUniqueID = ids.CollectionId,
                TestMethodUniqueID = ids.MethodId,
                TestUniqueID = testUniqueId,
                Warnings = null,
            };
        }

        IMessageSinkMessage verdict = outcome switch
        {
            StepOutcome.Passed => new TestPassed
            {
                AssemblyUniqueID = ids.AssemblyId,
                ExecutionTime = elapsed,
                FinishTime = finishTime,
                Output = string.Empty,
                TestCaseUniqueID = ids.CaseId,
                TestClassUniqueID = ids.ClassId,
                TestCollectionUniqueID = ids.CollectionId,
                TestMethodUniqueID = ids.MethodId,
                TestUniqueID = testUniqueId,
                Warnings = null,
            },
            StepOutcome.Skipped skipped => new TestSkipped
            {
                AssemblyUniqueID = ids.AssemblyId,
                ExecutionTime = elapsed,
                FinishTime = finishTime,
                Output = string.Empty,
                Reason = skipped.Reason,
                TestCaseUniqueID = ids.CaseId,
                TestClassUniqueID = ids.ClassId,
                TestCollectionUniqueID = ids.CollectionId,
                TestMethodUniqueID = ids.MethodId,
                TestUniqueID = testUniqueId,
                Warnings = null,
            },
            StepOutcome.Failed failed => MakeFailed(failed.Exception),
            _ => throw new NotSupportedException($"Unknown outcome: {outcome.GetType()}"),
        };

        await QueueOrCancel(messageBus, verdict, cts);

        await QueueOrCancel(messageBus, new TestFinished
        {
            AssemblyUniqueID = ids.AssemblyId,
            Attachments = FrozenDictionary<string, TestAttachment>.Empty,
            ExecutionTime = elapsed,
            FinishTime = finishTime,
            Output = string.Empty,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestMethodUniqueID = ids.MethodId,
            TestUniqueID = testUniqueId,
            Warnings = null,
        }, cts);
    }

    private async ValueTask EmitSynthetic(
        IMessageBus messageBus,
        CancellationTokenSource cts,
        MsgIds ids,
        string testUniqueId,
        string displayName,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> traits,
        decimal elapsed,
        StepOutcome outcome)
    {
        var now = DateTimeOffset.UtcNow;
        await EmitStarting(messageBus, cts, ids, testUniqueId, displayName, traits, now);
        await EmitOutcome(messageBus, cts, ids, testUniqueId, now, elapsed, outcome);
    }

    private readonly record struct MsgIds(
        string AssemblyId,
        string CollectionId,
        string? ClassId,
        string? MethodId,
        string CaseId);

    // Queues a message and cancels the run if the bus signals it should stop.
    private static async ValueTask QueueOrCancel(IMessageBus messageBus, IMessageSinkMessage message, CancellationTokenSource cts)
    {
        if (!messageBus.QueueMessage(message))
        {
            await cts.CancelAsync();
        }
    }

    // Reports a synthetic failure for a phase that failed outside the normal step loop
    // (config error, timeout, background throw, teardown throw).
    private ValueTask ReportSyntheticFailure(
        IMessageBus messageBus,
        CancellationTokenSource cts,
        MsgIds ids,
        string displayName,
        int stepIndex,
        Exception failure,
        decimal elapsed) =>
        EmitSynthetic(messageBus, cts, ids, UniqueIDGenerator.ForTest(ids.CaseId, stepIndex),
            displayName, this.Traits, elapsed, new StepOutcome.Failed(failure));

    private async ValueTask<RunSummary> RunDelayEnumerated(
        IMessageBus messageBus,
        object?[] constructorArguments,
        CancellationTokenSource cts,
        MsgIds ids)
    {
        var summary = new RunSummary();
        await using var disposalTracker = new DisposalTracker();

        foreach (var dataAttr in this.TestMethod.DataAttributes)
        {
            var rows = await dataAttr.GetData(this.TestMethod.Method, disposalTracker);
            foreach (var row in rows)
            {
                var args = row.GetData();
                summary.Aggregate(await this.RunWithArguments(messageBus, constructorArguments, args, cts, ids));
            }
        }

        return summary;
    }

    private bool IsSkipException(Exception ex) =>
        this.SkipExceptions is { } types && types.Any(t => t.IsInstanceOfType(ex));

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

    private static async ValueTask<(Exception? failure, decimal elapsedSeconds)> InvokeMethod(
        object instance,
        MethodInfo method)
    {
        var sw = Stopwatch.StartNew();
        Exception? failure = null;
        try
        {
            var result = method.Invoke(instance, null);
            if (result is Task task)
            {
                await task;
            }
        }
        catch (Exception ex)
        {
            failure = ex is TargetInvocationException tie ? tie.InnerException ?? tie : ex;
        }

        sw.Stop();
        return (failure, (decimal)sw.Elapsed.TotalSeconds);
    }

    // [Background]/[Teardown] resolution depends only on the test class type, so it is cached
    // once per type rather than re-scanned on every test case and every delay-enumerated row.
    private static readonly ConcurrentDictionary<Type, FixtureMethods> FixtureMethodsByType = new();

    private readonly record struct FixtureMethods(MethodInfo? Background, MethodInfo? Teardown, string? ConfigError);

    private static FixtureMethods ResolveFixtureMethods(Type testClass) =>
        FixtureMethodsByType.GetOrAdd(testClass, static type =>
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

    // GetParameters() clones an array on each call; the parameters are constant per test method.
    private ParameterInfo[] MethodParameters => field ??= this.TestMethod.Method.GetParameters();

    private async ValueTask<RunSummary> RunWithArguments(
        IMessageBus messageBus,
        object?[] constructorArguments,
        object?[]? methodArguments,
        CancellationTokenSource cts,
        MsgIds ids)
    {
        var summary = new RunSummary();

        var (backgroundMethod, teardownMethod, configError) = ResolveFixtureMethods(this.TestClass.Class);

        if (configError is not null)
        {
            await this.ReportSyntheticFailure(messageBus, cts, ids, "(Configuration Error)", stepIndex: 0,
                new InvalidOperationException(configError), elapsed: 0m);
            summary.Failed++;
            summary.Total++;
            return summary;
        }

        // Instantiate test class using constructor arguments provided by xUnit (resolved fixtures)
        var testClassInstance = constructorArguments.Length == 0
            ? Activator.CreateInstance(this.TestClass.Class)!
            : Activator.CreateInstance(this.TestClass.Class, constructorArguments)!;

        var mainStepCount = 0;
        var backgroundFailed = false;
        try
        {
            using var ctx = Scenario.Acquire();

            if (backgroundMethod != null)
            {
                var (bgFailure, bgElapsed) = await InvokeMethod(testClassInstance, backgroundMethod);
                if (bgFailure != null)
                {
                    summary.Time += bgElapsed;
                    summary.Total++;
                    if (this.IsSkipException(bgFailure))
                    {
                        await this.EmitSynthetic(messageBus, cts, ids,
                            UniqueIDGenerator.ForTest(ids.CaseId, 0), "(Background)", this.Traits, elapsed: 0m, new StepOutcome.Skipped(bgFailure.Message));
                        summary.Skipped++;
                    }
                    else
                    {
                        await this.ReportSyntheticFailure(messageBus, cts, ids, "(Background)", stepIndex: 0, bgFailure, bgElapsed);
                        summary.Failed++;
                    }

                    backgroundFailed = true;
                }
            }

            if (!backgroundFailed)
            {
                var invocationArguments = methodArguments;
                var parameters = this.MethodParameters;
                var providedCount = invocationArguments?.Length ?? 0;
                if (providedCount < parameters.Length)
                {
                    invocationArguments =
                    [
                        .. invocationArguments ?? [],
                        .. parameters.Skip(providedCount)
                            .Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null),
                    ];
                }

                var scenarioResult = this.TestMethod.Method.Invoke(testClassInstance, invocationArguments);
                if (scenarioResult is Task scenarioTask)
                {
                    await scenarioTask;
                }

                var mainSteps = Scenario.TestDefinitions.ToList();
                mainStepCount = mainSteps.Count;
                summary.Aggregate(await this.RunStepLoop(mainSteps, stepIndexOffset: 0, methodArguments, messageBus, cts, ids));
            }
        }
        finally
        {
            if (teardownMethod != null)
            {
                var teardownOffset = backgroundFailed ? 1 : mainStepCount;
                using var teardownCtx = Scenario.Acquire();

                try
                {
                    var (tdFailure, tdElapsed) = await InvokeMethod(testClassInstance, teardownMethod);
                    if (tdFailure != null)
                    {
                        summary.Time += tdElapsed;
                        summary.Total++;
                        if (this.IsSkipException(tdFailure))
                        {
                            await this.EmitSynthetic(messageBus, cts, ids,
                                UniqueIDGenerator.ForTest(ids.CaseId, teardownOffset), "(Teardown)", this.Traits, elapsed: 0m, new StepOutcome.Skipped(tdFailure.Message));
                            summary.Skipped++;
                        }
                        else
                        {
                            await this.ReportSyntheticFailure(messageBus, cts, ids, "(Teardown)", teardownOffset, tdFailure, tdElapsed);
                            summary.Failed++;
                        }
                        // fall through — do NOT return (would suppress in-flight exception)
                    }
                    else
                    {
                        var tdSteps = Scenario.TestDefinitions.ToList();
                        summary.Aggregate(await this.RunStepLoop(tdSteps, stepIndexOffset: teardownOffset, methodArguments, messageBus, cts, ids));
                    }
                }
                catch (Exception tdEx)
                {
                    // Teardown threw unexpectedly (e.g. from RunStepLoop or message bus). Record to summary
                    // but do not re-throw — this is a finally block, re-throwing would swallow any
                    // in-flight exception from the try block.
                    await this.ReportSyntheticFailure(messageBus, cts, ids, "(Teardown)", teardownOffset, tdEx, elapsed: 0m);
                    summary.Failed++;
                    summary.Total++;
                }
            }
        }

        return summary;
    }

    private async ValueTask<RunSummary> RunStepLoop(
        List<ScenarioTestDefinition> steps,
        int stepIndexOffset,
        object?[]? rowArgs,
        IMessageBus messageBus,
        CancellationTokenSource cts,
        MsgIds ids)
    {
        var summary = new RunSummary();
        var stopped = false;

        // Row arguments are identical for every step, so serialize them once per row rather than
        // re-serializing inside each step's UniqueID.
        var serializedRowArgs = rowArgs is { Length: > 0 }
            ? Array.ConvertAll(rowArgs, static arg => SerializeArgForId(arg))
            : null;

        for (var i = 0; i < steps.Count; i++)
        {
            var td = steps[i];
            var step = new ScenarioStep(this, stepIndexOffset + i, td.Tale, rowArgs, serializedRowArgs);
            var testUniqueId = step.UniqueID;
            summary.Total++;

            if (stopped)
            {
                summary.Skipped++;
                await this.EmitSynthetic(messageBus, cts, ids, testUniqueId, step.TestDisplayName, step.Traits, elapsed: 0m, new StepOutcome.Skipped("Previous step failed"));
                continue;
            }

            var start = DateTimeOffset.UtcNow;
            await this.EmitStarting(messageBus, cts, ids, testUniqueId, step.TestDisplayName, step.Traits, start);

            Exception? failure = null;
            var sw = Stopwatch.StartNew();

            try
            {
                switch (td.Lambda)
                {
                    case TaleBody.SynchronousTaleBody sync:
                        sync.Body.Invoke();
                        break;
                    case TaleBody.AsynchronousTaleBody asyncBody:
                        await asyncBody.Body.Invoke();
                        break;
                    default:
                        throw new NotSupportedException($"Unknown lambda type: {td.Lambda.GetType()}");
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            sw.Stop();
            var elapsed = (decimal)sw.Elapsed.TotalSeconds;
            var finish = DateTimeOffset.UtcNow;

            StepOutcome outcome;
            if (failure is null)
            {
                outcome = new StepOutcome.Passed();
            }
            else if (this.IsSkipException(failure))
            {
                summary.Skipped++;
                outcome = new StepOutcome.Skipped(failure.Message);
            }
            else
            {
                summary.Failed++;
                if (td.OnError == OnError.Stop)
                {
                    stopped = true;
                }

                outcome = new StepOutcome.Failed(failure);
            }

            summary.Time += elapsed;

            await this.EmitOutcome(messageBus, cts, ids, testUniqueId, finish, elapsed, outcome);
        }

        return summary;
    }
}
