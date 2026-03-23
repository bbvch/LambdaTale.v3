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

    private static readonly IReadOnlyDictionary<string, TestAttachment> EmptyAttachments =
        new Dictionary<string, TestAttachment>();

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
        bool skipTestWithoutData = false)
    {
        this.TestMethod = Guard.ArgumentNotNull(testMethod);
        this.TestMethodArguments = testMethodArguments;
        this.testCaseDisplayName = testCaseDisplayName;
        this.SkipReason = skipReason;
        this.SourceFilePath = sourceFilePath;
        this.SourceLineNumber = sourceLineNumber;
        this.isDelayEnumerated = isDelayEnumerated;
        this.skipTestWithoutData = skipTestWithoutData;
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

    public string[] TestMethodParameterTypesVSTest => this.TestMethod.Parameters.Select(p => p.ParameterType.FullName ?? p.ParameterType.Name).ToArray();

    public string TestMethodReturnTypeVSTest => this.TestMethod.ReturnType.FullName ?? this.TestMethod.ReturnType.Name;

    public int? TestMethodArity =>
        this.TestMethod.Method.IsGenericMethodDefinition
            ? this.TestMethod.Method.GetGenericArguments().Length
            : null;

    public string? SkipReason { get; private set; }
    public Type? SkipType => null;
    public string? SkipUnless => null;
    public string? SkipWhen => null;
    public Type[]? SkipExceptions => null;
    public int Timeout => 0;

    public string? SourceFilePath { get; private set; }
    public int? SourceLineNumber { get; private set; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => this.TestMethod.Traits;

    public ValueTask<IReadOnlyCollection<IXunitTest>> CreateTests() => new([]);
    public void PreInvoke() { }
    public void PostInvoke() { }

    ITestClass ITestCase.TestClass => this.TestClass;
    ITestCollection ITestCase.TestCollection => this.TestCollection;
    ITestMethod ITestCase.TestMethod => this.TestMethod;
    bool ITestCaseMetadata.Explicit => false;
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
        ExplicitOption _,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator __,
        CancellationTokenSource cancellationTokenSource)
    {
        var assemblyUniqueId = this.TestCollection.TestAssembly.UniqueID;
        var testCollectionUniqueId = this.TestCollection.UniqueID;
        var testClassUniqueId = this.TestClass.UniqueID;
        var testMethodUniqueId = this.TestMethod.UniqueID;
        var testCaseUniqueId = this.UniqueID;
        RunSummary summary;

        if (!messageBus.QueueMessage(new TestCaseStarting
            {
                AssemblyUniqueID = assemblyUniqueId,
                Explicit = false,
                SkipReason = this.SkipReason,
                SourceFilePath = this.SourceFilePath,
                SourceLineNumber = this.SourceLineNumber,
                TestCaseDisplayName = this.TestCaseDisplayName,
                TestCaseUniqueID = testCaseUniqueId,
                TestClassMetadataToken = this.TestClassMetadataToken,
                TestClassName = this.TestClassName,
                TestClassNamespace = this.TestMethod.TestClass.Class.Namespace,
                TestClassSimpleName = this.TestClassSimpleName,
                TestClassUniqueID = testClassUniqueId,
                TestCollectionUniqueID = testCollectionUniqueId,
                TestMethodArity = this.TestMethodArity,
                TestMethodMetadataToken = this.TestMethodMetadataToken,
                TestMethodName = this.TestMethodName,
                TestMethodParameterTypesVSTest = this.TestMethodParameterTypesVSTest,
                TestMethodReturnTypeVSTest = this.TestMethodReturnTypeVSTest,
                TestMethodUniqueID = testMethodUniqueId,
                Traits = this.Traits,
            }))
        {
            await cancellationTokenSource.CancelAsync();
        }

        if (this.SkipReason is not null)
        {
            summary = this.SendSkippedTestCase(messageBus, cancellationTokenSource,
                assemblyUniqueId, testCollectionUniqueId, testClassUniqueId, testMethodUniqueId, testCaseUniqueId);
        }
        else if (this.isDelayEnumerated)
        {
            summary = await this.RunDelayEnumerated(messageBus, constructorArguments, cancellationTokenSource,
                assemblyUniqueId, testCollectionUniqueId, testClassUniqueId, testMethodUniqueId, testCaseUniqueId);
        }
        else
        {
            summary = await this.RunWithArguments(messageBus, constructorArguments, this.TestMethodArguments, cancellationTokenSource,
                assemblyUniqueId, testCollectionUniqueId, testClassUniqueId, testMethodUniqueId, testCaseUniqueId);
        }

        if (!messageBus.QueueMessage(new TestCaseFinished
            {
                AssemblyUniqueID = assemblyUniqueId,
                ExecutionTime = summary.Time,
                TestCaseUniqueID = testCaseUniqueId,
                TestClassUniqueID = testClassUniqueId,
                TestCollectionUniqueID = testCollectionUniqueId,
                TestMethodUniqueID = testMethodUniqueId,
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

    private RunSummary SendSkippedTestCase(
        IMessageBus messageBus,
        CancellationTokenSource cts,
        string assemblyId,
        string collectionId,
        string? classId,
        string? methodId,
        string caseId)
    {
        var testUniqueId = UniqueIDGenerator.ForTest(caseId, 0);
        var now = DateTimeOffset.UtcNow;

        if (!messageBus.QueueMessage(new TestStarting
            {
                AssemblyUniqueID = assemblyId,
                Explicit = false,
                StartTime = now,
                TestCaseUniqueID = caseId,
                TestClassUniqueID = classId,
                TestCollectionUniqueID = collectionId,
                TestDisplayName = this.TestCaseDisplayName,
                TestMethodUniqueID = methodId,
                TestUniqueID = testUniqueId,
                Timeout = 0,
                Traits = this.Traits,
            }))
        {
            cts.Cancel();
        }

        if (!messageBus.QueueMessage(new TestSkipped
            {
                AssemblyUniqueID = assemblyId,
                ExecutionTime = 0m,
                FinishTime = now,
                Output = string.Empty,
                Reason = this.SkipReason!,
                TestCaseUniqueID = caseId,
                TestClassUniqueID = classId,
                TestCollectionUniqueID = collectionId,
                TestMethodUniqueID = methodId,
                TestUniqueID = testUniqueId,
                Warnings = null,
            }))
        {
            cts.Cancel();
        }

        if (!messageBus.QueueMessage(new TestFinished
            {
                AssemblyUniqueID = assemblyId,
                Attachments = EmptyAttachments,
                ExecutionTime = 0m,
                FinishTime = now,
                Output = string.Empty,
                TestCaseUniqueID = caseId,
                TestClassUniqueID = classId,
                TestCollectionUniqueID = collectionId,
                TestMethodUniqueID = methodId,
                TestUniqueID = testUniqueId,
                Warnings = null,
            }))
        {
            cts.Cancel();
        }

        return new RunSummary { Total = 1, Skipped = 1 };
    }

    private async ValueTask<RunSummary> RunDelayEnumerated(
        IMessageBus messageBus,
        object?[] constructorArguments,
        CancellationTokenSource cts,
        string assemblyId,
        string collectionId,
        string? classId,
        string? methodId,
        string caseId)
    {
        var summary = new RunSummary();
        await using var disposalTracker = new DisposalTracker();

        foreach (var dataAttr in this.TestMethod.DataAttributes)
        {
            var rows = await dataAttr.GetData(this.TestMethod.Method, disposalTracker);
            foreach (var row in rows)
            {
                var args = row.GetData();
                var rowSummary = await this.RunWithArguments(messageBus, constructorArguments, args, cts,
                    assemblyId, collectionId, classId, methodId, caseId);
                summary.Total += rowSummary.Total;
                summary.Failed += rowSummary.Failed;
                summary.Skipped += rowSummary.Skipped;
                summary.Time += rowSummary.Time;
            }
        }

        return summary;
    }

    // Bundles the five IDs that appear in every xunit message, reducing parameter noise.
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
            Explicit = false,
            StartTime = now,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestDisplayName = displayName,
            TestMethodUniqueID = ids.MethodId,
            TestUniqueID = uniqueId,
            Timeout = 0,
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
            Attachments = EmptyAttachments,
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

    private async ValueTask<RunSummary> RunWithArguments(
        IMessageBus messageBus,
        object?[] constructorArguments,
        object?[]? methodArguments,
        CancellationTokenSource cts,
        string assemblyId,
        string collectionId,
        string? classId,
        string? methodId,
        string caseId)
    {
        var summary = new RunSummary();
        var ids = new MsgIds(assemblyId, collectionId, classId, methodId, caseId);

        // Instantiate test class using constructor arguments provided by xUnit (resolved fixtures)
        var testClassInstance = constructorArguments.Length == 0
            ? Activator.CreateInstance(this.TestClass.Class)!
            : Activator.CreateInstance(this.TestClass.Class, constructorArguments)!;

        // Discover [Background] and [Teardown] methods
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
                var bgSw = Stopwatch.StartNew();
                Exception? bgFailure = null;
                try
                {
                    var bgResult = backgroundMethod.Invoke(testClassInstance, null);
                    if (bgResult is Task bgTask)
                    {
                        await bgTask;
                    }
                }
                catch (Exception ex)
                {
                    bgFailure = ex is TargetInvocationException tie ? tie.InnerException ?? tie : ex;
                }

                bgSw.Stop();

                if (bgFailure != null)
                {
                    var bgElapsed = (decimal)bgSw.Elapsed.TotalSeconds;
                    this.ReportSyntheticFailure(messageBus, ids, "(Background)", stepIndex: 0, bgFailure, bgElapsed);
                    summary.Time += bgElapsed;
                    summary.Failed++;
                    summary.Total++;
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

                var mainSteps = Scenario.TestDefinitions.OrderBy(td => td.index).ToList();
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
                    Exception? tdFailure = null;
                    var tdSw = Stopwatch.StartNew();
                    try
                    {
                        var tdResult = teardownMethod.Invoke(testClassInstance, null);
                        if (tdResult is Task tdTask)
                        {
                            await tdTask;
                        }
                    }
                    catch (Exception ex)
                    {
                        tdFailure = ex is TargetInvocationException tie ? tie.InnerException ?? tie : ex;
                    }

                    tdSw.Stop();

                    if (tdFailure != null)
                    {
                        var tdElapsed = (decimal)tdSw.Elapsed.TotalSeconds;
                        this.ReportSyntheticFailure(messageBus, ids, "(Teardown)", teardownOffset, tdFailure, tdElapsed);
                        summary.Time += tdElapsed;
                        summary.Failed++;
                        summary.Total++;
                        // fall through — do NOT return (would suppress in-flight exception)
                    }
                    else
                    {
                        var tdSteps = Scenario.TestDefinitions.OrderBy(td => td.index).ToList();
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
                SendSkippedStep(messageBus, cts, step, testUniqueId, ids, "Previous step failed");
                continue;
            }

            var start = DateTimeOffset.UtcNow;
            if (!messageBus.QueueMessage(new TestStarting
                {
                    AssemblyUniqueID = ids.AssemblyId,
                    Explicit = false,
                    StartTime = start,
                    TestCaseUniqueID = ids.CaseId,
                    TestClassUniqueID = ids.ClassId,
                    TestCollectionUniqueID = ids.CollectionId,
                    TestDisplayName = step.TestDisplayName,
                    TestMethodUniqueID = ids.MethodId,
                    TestUniqueID = testUniqueId,
                    Timeout = 0,
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
                summary.Failed++;
                if (td.OnError == OnError.Stop)
                {
                    stopped = true;
                }
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
            else
            {
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
                    Attachments = EmptyAttachments,
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

    private static void SendSkippedStep(
        IMessageBus messageBus,
        CancellationTokenSource cts,
        ScenarioStep step,
        string testUniqueId,
        MsgIds ids,
        string reason)
    {
        var now = DateTimeOffset.UtcNow;

        if (!messageBus.QueueMessage(new TestStarting
            {
                AssemblyUniqueID = ids.AssemblyId,
                Explicit = false,
                StartTime = now,
                TestCaseUniqueID = ids.CaseId,
                TestClassUniqueID = ids.ClassId,
                TestCollectionUniqueID = ids.CollectionId,
                TestDisplayName = step.TestDisplayName,
                TestMethodUniqueID = ids.MethodId,
                TestUniqueID = testUniqueId,
                Timeout = 0,
                Traits = step.Traits,
            }))
        {
            cts.Cancel();
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
            cts.Cancel();
        }

        if (!messageBus.QueueMessage(new TestFinished
            {
                AssemblyUniqueID = ids.AssemblyId,
                Attachments = EmptyAttachments,
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
            cts.Cancel();
        }
    }
}
