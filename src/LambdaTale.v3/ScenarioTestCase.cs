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
    private bool @explicit;
    private Type[]? skipExceptions;
    private Type? skipType;
    private string? skipUnless;
    private string? skipWhen;
    private int timeout;
    private string[]? testMethodParameterTypesVSTest;

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
        bool @explicit = false,
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
        this.@explicit = @explicit;
        this.skipExceptions = skipExceptions;
        this.skipType = skipType;
        this.skipUnless = skipUnless;
        this.skipWhen = skipWhen;
        this.timeout = timeout;
    }

    public IXunitTestMethod TestMethod
    {
        get =>
            field ?? throw new InvalidOperationException($"Uninitialized {nameof(ScenarioTestCase)}.{nameof(this.TestMethod)}");
        private set;
    }

    public object?[]? TestMethodArguments { get; private set; }

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
                    g.Add(SerializationHelper.Instance.Serialize(arg));
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
        this.testMethodParameterTypesVSTest ??=
            this.TestMethod.Parameters.Select(p => p.ParameterType.FullName ?? p.ParameterType.Name).ToArray();

    public string TestMethodReturnTypeVSTest => this.TestMethod.ReturnType.FullName ?? this.TestMethod.ReturnType.Name;

    public int? TestMethodArity =>
        this.TestMethod.Method.IsGenericMethodDefinition
            ? this.TestMethod.Method.GetGenericArguments().Length
            : null;

    public string? SkipReason { get; private set; }
    public Type? SkipType => this.skipType;
    public string? SkipUnless => this.skipUnless;
    public string? SkipWhen => this.skipWhen;
    public Type[]? SkipExceptions => this.skipExceptions;
    public int Timeout => this.timeout;

    public string? SourceFilePath { get; private set; }
    public int? SourceLineNumber { get; private set; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.TestMethod.Traits;

    public ValueTask<IReadOnlyCollection<IXunitTest>> CreateTests() => new([]);
    public void PreInvoke() { }
    public void PostInvoke() { }

    ITestClass ITestCase.TestClass => this.TestClass;
    ITestCollection ITestCase.TestCollection => this.TestCollection;
    ITestMethod ITestCase.TestMethod => this.TestMethod;
    bool ITestCaseMetadata.Explicit => this.@explicit;
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
        info.AddValue("ex", this.@explicit);
        var skipExc = this.skipExceptions?.Select(t => t.AssemblyQualifiedName ?? t.FullName ?? t.Name).ToArray();
        info.AddValue("sx", skipExc);
        info.AddValue("st", this.skipType?.AssemblyQualifiedName ?? this.skipType?.FullName);
        info.AddValue("su", this.skipUnless);
        info.AddValue("sw", this.skipWhen);
        info.AddValue("to", this.timeout);
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
        this.@explicit = info.GetValue<bool>("ex");
        var skipExc = info.GetValue<string[]?>("sx");
        this.skipExceptions = skipExc?.Select(name => Type.GetType(name, throwOnError: true)!).ToArray();
        var skipTypeName = info.GetValue<string?>("st");
        this.skipType = skipTypeName is null ? null : Type.GetType(skipTypeName, throwOnError: true);
        this.skipUnless = info.GetValue<string?>("su");
        this.skipWhen = info.GetValue<string?>("sw");
        this.timeout = info.GetValue<int>("to");
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

        var explicitSkipReason = (this.@explicit, explicitOption) switch
        {
            (true, ExplicitOption.Off) => "Test is marked Explicit and was not selected to run",
            (false, ExplicitOption.Only) => "Only explicit tests were selected to run",
            _ => null,
        };
        var conditionalSkipReason = this.EvaluateConditionalSkip();
        var effectiveSkipReason = this.SkipReason ?? conditionalSkipReason ?? explicitSkipReason;

        if (!messageBus.QueueMessage(new TestCaseStarting
            {
                AssemblyUniqueID = ids.AssemblyId,
                Explicit = this.@explicit,
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
            }))
        {
            await cancellationTokenSource.CancelAsync();
        }

        if (effectiveSkipReason is not null)
        {
            summary = await this.SendSkippedTestCase(messageBus, cancellationTokenSource, ids, effectiveSkipReason);
        }
        else
        {
            var dispatch = this.isDelayEnumerated
                ? this.RunDelayEnumerated(messageBus, constructorArguments, cancellationTokenSource, ids).AsTask()
                : this.RunWithArguments(messageBus, constructorArguments, this.TestMethodArguments, cancellationTokenSource, ids).AsTask();

            if (this.timeout > 0)
            {
                var winner = await Task.WhenAny(dispatch, Task.Delay(this.timeout));
                if (winner != dispatch)
                {
                    var elapsed = this.timeout / 1000m;
                    var timeoutEx = new TimeoutException($"Test exceeded timeout of {this.timeout}ms");
                    this.ReportSyntheticFailure(messageBus, ids, "(Timeout)", stepIndex: 0, timeoutEx, elapsed);
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

        if (!messageBus.QueueMessage(new TestCaseFinished
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
            }))
        {
            await cancellationTokenSource.CancelAsync();
        }

        return summary;
    }

    private async ValueTask<RunSummary> SendSkippedTestCase(
        IMessageBus messageBus,
        CancellationTokenSource cts,
        MsgIds ids,
        string skipReason)
    {
        var testUniqueId = UniqueIDGenerator.ForTest(ids.CaseId, 0);
        await SendSkippedMessages(messageBus, cts, ids, testUniqueId, this.TestCaseDisplayName, this.Traits, this.@explicit, this.timeout, skipReason);
        return new RunSummary { Total = 1, Skipped = 1 };
    }

    private static async ValueTask SendSkippedMessages(
        IMessageBus messageBus,
        CancellationTokenSource cts,
        MsgIds ids,
        string testUniqueId,
        string displayName,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> traits,
        bool @explicit,
        int timeout,
        string reason)
    {
        var now = DateTimeOffset.UtcNow;

        if (!messageBus.QueueMessage(new TestStarting
            {
                AssemblyUniqueID = ids.AssemblyId,
                Explicit = @explicit,
                StartTime = now,
                TestCaseUniqueID = ids.CaseId,
                TestClassUniqueID = ids.ClassId,
                TestCollectionUniqueID = ids.CollectionId,
                TestDisplayName = displayName,
                TestMethodUniqueID = ids.MethodId,
                TestUniqueID = testUniqueId,
                Timeout = timeout,
                Traits = traits,
            }))
        {
            await cts.CancelAsync();
        }

        if (!messageBus.QueueMessage(new TestSkipped
            {
                AssemblyUniqueID = ids.AssemblyId,
                ExecutionTime = 0m,
                FinishTime = now,
                Output = string.Empty,
                Reason = reason,
                TestCaseUniqueID = ids.CaseId,
                TestClassUniqueID = ids.ClassId,
                TestCollectionUniqueID = ids.CollectionId,
                TestMethodUniqueID = ids.MethodId,
                TestUniqueID = testUniqueId,
                Warnings = null,
            }))
        {
            await cts.CancelAsync();
        }

        if (!messageBus.QueueMessage(new TestFinished
            {
                AssemblyUniqueID = ids.AssemblyId,
                Attachments = FrozenDictionary<string, TestAttachment>.Empty,
                ExecutionTime = 0m,
                FinishTime = now,
                Output = string.Empty,
                TestCaseUniqueID = ids.CaseId,
                TestClassUniqueID = ids.ClassId,
                TestCollectionUniqueID = ids.CollectionId,
                TestMethodUniqueID = ids.MethodId,
                TestUniqueID = testUniqueId,
                Warnings = null,
            }))
        {
            await cts.CancelAsync();
        }
    }

    private readonly record struct MsgIds(
        string AssemblyId,
        string CollectionId,
        string? ClassId,
        string? MethodId,
        string CaseId);

    // Reports a synthetic TestStarting / TestFailed / TestFinished triple for a phase that
    // failed before any steps were registered (config error, background throw, teardown throw).
    private void ReportSyntheticFailure(
        IMessageBus messageBus,
        MsgIds ids,
        string displayName,
        int stepIndex,
        Exception failure,
        decimal elapsed)
    {
        var uniqueId = UniqueIDGenerator.ForTest(ids.CaseId, stepIndex);
        var now = DateTimeOffset.UtcNow;
        _ = messageBus.QueueMessage(new TestStarting
        {
            AssemblyUniqueID = ids.AssemblyId,
            Explicit = this.@explicit,
            StartTime = now,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestDisplayName = displayName,
            TestMethodUniqueID = ids.MethodId,
            TestUniqueID = uniqueId,
            Timeout = this.timeout,
            Traits = this.Traits,
        });
        var (types, messages, stackTraces, indices, cause) = ExceptionUtility.ExtractMetadata(failure);
        _ = messageBus.QueueMessage(new TestFailed
        {
            AssemblyUniqueID = ids.AssemblyId,
            Cause = cause,
            ExceptionParentIndices = indices,
            ExceptionTypes = types,
            ExecutionTime = elapsed,
            FinishTime = now,
            Messages = messages,
            Output = string.Empty,
            StackTraces = stackTraces,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestMethodUniqueID = ids.MethodId,
            TestUniqueID = uniqueId,
            Warnings = null,
        });
        _ = messageBus.QueueMessage(new TestFinished
        {
            AssemblyUniqueID = ids.AssemblyId,
            Attachments = FrozenDictionary<string, TestAttachment>.Empty,
            ExecutionTime = elapsed,
            FinishTime = now,
            Output = string.Empty,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestMethodUniqueID = ids.MethodId,
            TestUniqueID = uniqueId,
            Warnings = null,
        });
    }

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
        this.skipExceptions is { } types && types.Any(t => t.IsInstanceOfType(ex));

    private string? EvaluateConditionalSkip()
    {
        if (this.skipUnless is null && this.skipWhen is null)
        {
            return null;
        }

        if (this.skipUnless is not null && this.skipWhen is not null)
        {
            throw new InvalidOperationException("Only one of SkipUnless or SkipWhen may be set.");
        }

        var propertyName = this.skipUnless ?? this.skipWhen!;
        var hostType = this.skipType ?? this.TestClass.Class;
        var property = hostType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"Could not find public static property '{propertyName}' on type '{hostType.FullName}'.");
        var value = property.GetValue(obj: null);
        var truthy = value is true;

        var shouldSkip = this.skipUnless is not null ? !truthy : truthy;
        if (!shouldSkip)
        {
            return null;
        }

        return this.SkipReason ?? $"Conditional skip ({propertyName})";
    }

    private static async ValueTask<(Exception? failure, decimal elapsedSeconds)> InvokeMethod(
        object instance, MethodInfo method)
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

    private async ValueTask<RunSummary> RunWithArguments(
        IMessageBus messageBus,
        object?[] constructorArguments,
        object?[]? methodArguments,
        CancellationTokenSource cts,
        MsgIds ids)
    {
        var summary = new RunSummary();

        // Instantiate test class using constructor arguments provided by xUnit (resolved fixtures)
        var testClassInstance = constructorArguments.Length == 0
            ? Activator.CreateInstance(this.TestClass.Class)!
            : Activator.CreateInstance(this.TestClass.Class, constructorArguments)!;

        var allMethods = testClassInstance.GetType().GetMethods(
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var backgroundMethods = allMethods.Where(m => m.GetCustomAttribute<BackgroundAttribute>() != null).ToList();
        var teardownMethods = allMethods.Where(m => m.GetCustomAttribute<TeardownAttribute>() != null).ToList();

        if (backgroundMethods.Count > 1 || teardownMethods.Count > 1)
        {
            var offenders = new List<string>();
            if (backgroundMethods.Count > 1)
            {
                offenders.Add(nameof(BackgroundAttribute));
            }

            if (teardownMethods.Count > 1)
            {
                offenders.Add(nameof(TeardownAttribute));
            }

            var which = string.Join(" and ", offenders.Select(o => $"[{o}]"));
            var msg = $"Multiple {which} methods found. Only one is allowed per class.";
            this.ReportSyntheticFailure(messageBus, ids, "(Configuration Error)", stepIndex: 0,
                new InvalidOperationException(msg), elapsed: 0m);
            summary.Failed++;
            summary.Total++;
            return summary;
        }

        var backgroundMethod = backgroundMethods.SingleOrDefault();
        var teardownMethod = teardownMethods.SingleOrDefault();

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
                        await SendSkippedMessages(messageBus, cts, ids,
                            UniqueIDGenerator.ForTest(ids.CaseId, 0), "(Background)", this.Traits, this.@explicit, this.timeout, bgFailure.Message);
                        summary.Skipped++;
                    }
                    else
                    {
                        this.ReportSyntheticFailure(messageBus, ids, "(Background)", stepIndex: 0, bgFailure, bgElapsed);
                        summary.Failed++;
                    }

                    backgroundFailed = true;
                }
            }

            if (!backgroundFailed)
            {
                var invocationArguments = methodArguments;
                var parameters = this.TestMethod.Method.GetParameters();
                if (invocationArguments is null)
                {
                    if (parameters.Length > 0)
                    {
                        invocationArguments = [.. parameters.Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)];
                    }
                }
                else if (invocationArguments.Length < parameters.Length)
                {
                    invocationArguments =
                    [
                        .. invocationArguments,
                        .. parameters.Skip(invocationArguments.Length)
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
                summary.Aggregate(await this.RunStepLoop(mainSteps, stepIndexOffset: 0, messageBus, cts, ids));
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
                            await SendSkippedMessages(messageBus, cts, ids,
                                UniqueIDGenerator.ForTest(ids.CaseId, teardownOffset), "(Teardown)", this.Traits, this.@explicit, this.timeout, tdFailure.Message);
                            summary.Skipped++;
                        }
                        else
                        {
                            this.ReportSyntheticFailure(messageBus, ids, "(Teardown)", teardownOffset, tdFailure, tdElapsed);
                            summary.Failed++;
                        }
                        // fall through — do NOT return (would suppress in-flight exception)
                    }
                    else
                    {
                        var tdSteps = Scenario.TestDefinitions.ToList();
                        summary.Aggregate(await this.RunStepLoop(tdSteps, stepIndexOffset: teardownOffset, messageBus, cts, ids));
                    }
                }
                catch (Exception tdEx)
                {
                    // Teardown threw unexpectedly (e.g. from RunStepLoop or message bus). Record to summary
                    // but do not re-throw — this is a finally block, re-throwing would swallow any
                    // in-flight exception from the try block.
                    this.ReportSyntheticFailure(messageBus, ids, "(Teardown)", teardownOffset, tdEx, elapsed: 0m);
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
        IMessageBus messageBus,
        CancellationTokenSource cts,
        MsgIds ids)
    {
        var summary = new RunSummary();
        var stopped = false;

        for (var i = 0; i < steps.Count; i++)
        {
            var td = steps[i];
            var step = new ScenarioStep(this, stepIndexOffset + i, td.Tale);
            var testUniqueId = step.UniqueID;
            summary.Total++;

            if (stopped)
            {
                summary.Skipped++;
                await SendSkippedMessages(messageBus, cts, ids, testUniqueId, step.TestDisplayName, step.Traits, this.@explicit, this.timeout, "Previous step failed");
                continue;
            }

            var start = DateTimeOffset.UtcNow;
            if (!messageBus.QueueMessage(new TestStarting
                {
                    AssemblyUniqueID = ids.AssemblyId,
                    Explicit = this.@explicit,
                    StartTime = start,
                    TestCaseUniqueID = ids.CaseId,
                    TestClassUniqueID = ids.ClassId,
                    TestCollectionUniqueID = ids.CollectionId,
                    TestDisplayName = step.TestDisplayName,
                    TestMethodUniqueID = ids.MethodId,
                    TestUniqueID = testUniqueId,
                    Timeout = this.timeout,
                    Traits = step.Traits,
                }))
            {
                await cts.CancelAsync();
            }

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

            if (failure is null)
            {
                if (!messageBus.QueueMessage(new TestPassed
                    {
                        AssemblyUniqueID = ids.AssemblyId,
                        ExecutionTime = elapsed,
                        FinishTime = finish,
                        Output = string.Empty,
                        TestCaseUniqueID = ids.CaseId,
                        TestClassUniqueID = ids.ClassId,
                        TestCollectionUniqueID = ids.CollectionId,
                        TestMethodUniqueID = ids.MethodId,
                        TestUniqueID = testUniqueId,
                        Warnings = null,
                    }))
                {
                    await cts.CancelAsync();
                }
            }
            else if (this.IsSkipException(failure))
            {
                summary.Skipped++;
                if (!messageBus.QueueMessage(new TestSkipped
                    {
                        AssemblyUniqueID = ids.AssemblyId,
                        ExecutionTime = elapsed,
                        FinishTime = finish,
                        Output = string.Empty,
                        Reason = failure.Message,
                        TestCaseUniqueID = ids.CaseId,
                        TestClassUniqueID = ids.ClassId,
                        TestCollectionUniqueID = ids.CollectionId,
                        TestMethodUniqueID = ids.MethodId,
                        TestUniqueID = testUniqueId,
                        Warnings = null,
                    }))
                {
                    await cts.CancelAsync();
                }
            }
            else
            {
                summary.Failed++;
                if (td.OnError == OnError.Stop)
                {
                    stopped = true;
                }

                var (types, messages, stackTraces, indices, cause) = ExceptionUtility.ExtractMetadata(failure);
                if (!messageBus.QueueMessage(new TestFailed
                    {
                        AssemblyUniqueID = ids.AssemblyId,
                        Cause = cause,
                        ExceptionParentIndices = indices,
                        ExceptionTypes = types,
                        ExecutionTime = elapsed,
                        FinishTime = finish,
                        Messages = messages,
                        Output = string.Empty,
                        StackTraces = stackTraces,
                        TestCaseUniqueID = ids.CaseId,
                        TestClassUniqueID = ids.ClassId,
                        TestCollectionUniqueID = ids.CollectionId,
                        TestMethodUniqueID = ids.MethodId,
                        TestUniqueID = testUniqueId,
                        Warnings = null,
                    }))
                {
                    await cts.CancelAsync();
                }
            }

            summary.Time += elapsed;

            if (!messageBus.QueueMessage(new TestFinished
                {
                    AssemblyUniqueID = ids.AssemblyId,
                    Attachments = FrozenDictionary<string, TestAttachment>.Empty,
                    ExecutionTime = elapsed,
                    FinishTime = finish,
                    Output = string.Empty,
                    TestCaseUniqueID = ids.CaseId,
                    TestClassUniqueID = ids.ClassId,
                    TestCollectionUniqueID = ids.CollectionId,
                    TestMethodUniqueID = ids.MethodId,
                    TestUniqueID = testUniqueId,
                    Warnings = null,
                }))
            {
                await cts.CancelAsync();
            }
        }

        return summary;
    }
}
