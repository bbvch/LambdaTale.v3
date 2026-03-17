using System.Diagnostics;
using System.Linq;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

[DebuggerDisplay(@"\{ class = {TestMethod.TestClass.TestClassName}, method = {TestMethod.MethodName}, display = {TestCaseDisplayName} \}")]
public sealed class ScenarioTestCase : IXunitTestCase, ISelfExecutingXunitTestCase, IXunitDelayEnumeratedTestCase, IXunitSerializable
{
    private IXunitTestMethod? _testMethod;
    private object?[]? _testMethodArguments;
    private string? _testCaseDisplayName;
    private string? _skipReason;
    private string? _sourceFilePath;
    private int? _sourceLineNumber;
    private bool _isDelayEnumerated;
    private bool _skipTestWithoutData;
    private string? _uniqueId;

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
        _testMethod = Guard.ArgumentNotNull(testMethod);
        _testMethodArguments = testMethodArguments;
        _testCaseDisplayName = testCaseDisplayName;
        _skipReason = skipReason;
        _sourceFilePath = sourceFilePath;
        _sourceLineNumber = sourceLineNumber;
        _isDelayEnumerated = isDelayEnumerated;
        _skipTestWithoutData = skipTestWithoutData;
    }

    // ── Core properties ────────────────────────────────────────────────────────

    public IXunitTestMethod TestMethod =>
        _testMethod ?? throw new InvalidOperationException($"Uninitialized {nameof(ScenarioTestCase)}.{nameof(TestMethod)}");

    public object?[]? TestMethodArguments => _testMethodArguments;

    public string UniqueID
    {
        get
        {
            if (_uniqueId is not null) return _uniqueId;
            using var g = new UniqueIDGenerator();
            g.Add(TestMethod.UniqueID);
            if (_testMethodArguments is not null)
                foreach (var arg in _testMethodArguments)
                    g.Add(SerializationHelper.Instance.Serialize(arg));
            if (_isDelayEnumerated)
                g.Add("delayed");
            return _uniqueId = g.Compute();
        }
    }

    public string TestCaseDisplayName =>
        _testCaseDisplayName ?? TestMethod.GetDisplayName(TestMethod.MethodName, null, _testMethodArguments, null);

    // ── IXunitTestCase ─────────────────────────────────────────────────────────

    public IXunitTestClass TestClass => TestMethod.TestClass;
    public IXunitTestCollection TestCollection => TestMethod.TestClass.TestCollection;

    public int TestClassMetadataToken => TestMethod.TestClass.Class.MetadataToken;
    public string TestClassName => TestMethod.TestClass.TestClassName;
    public string TestClassSimpleName => TestMethod.TestClass.TestClassSimpleName;

    public int TestMethodMetadataToken => TestMethod.Method.MetadataToken;
    public string TestMethodName => TestMethod.MethodName;

    public string[] TestMethodParameterTypesVSTest =>
        TestMethod.Parameters.Select(p => p.ParameterType.FullName ?? p.ParameterType.Name).ToArray();

    public string TestMethodReturnTypeVSTest =>
        TestMethod.ReturnType.FullName ?? TestMethod.ReturnType.Name;

    public int? TestMethodArity => TestMethod.Method.IsGenericMethodDefinition
        ? TestMethod.Method.GetGenericArguments().Length
        : (int?)null;

    public string? SkipReason => _skipReason;
    public Type? SkipType => null;
    public string? SkipUnless => null;
    public string? SkipWhen => null;
    public Type[]? SkipExceptions => null;
    public int Timeout => 0;

    public string? SourceFilePath => _sourceFilePath;
    public int? SourceLineNumber => _sourceLineNumber;

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestMethod.Traits;

    // IXunitTestCase members that are not used by ISelfExecutingXunitTestCase
    public ValueTask<IReadOnlyCollection<IXunitTest>> CreateTests() => new([]);
    public void PreInvoke() { }
    public void PostInvoke() { }

    // ITestCase (non-xunit base)
    ITestClass ITestCase.TestClass => TestClass;
    ITestCollection ITestCase.TestCollection => TestCollection;
    ITestMethod ITestCase.TestMethod => TestMethod;
    bool ITestCaseMetadata.Explicit => false;
    string? ITestCaseMetadata.SkipReason => _skipReason;
    int? ITestCaseMetadata.TestClassMetadataToken => TestClassMetadataToken;
    string? ITestCaseMetadata.TestClassNamespace => TestMethod.TestClass.Class.Namespace;
    int? ITestCaseMetadata.TestMethodMetadataToken => TestMethodMetadataToken;
    string[]? ITestCaseMetadata.TestMethodParameterTypesVSTest => TestMethodParameterTypesVSTest;
    string? ITestCaseMetadata.TestMethodReturnTypeVSTest => TestMethodReturnTypeVSTest;

    // IXunitDelayEnumeratedTestCase
    bool IXunitDelayEnumeratedTestCase.SkipTestWithoutData => _skipTestWithoutData;

    // ── Serialization ──────────────────────────────────────────────────────────

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("tm", TestMethod);
        info.AddValue("dn", _testCaseDisplayName);
        info.AddValue("sr", _skipReason);
        info.AddValue("sf", _sourceFilePath);
        info.AddValue("sl", _sourceLineNumber);
        info.AddValue("de", _isDelayEnumerated);
        info.AddValue("swd", _skipTestWithoutData);
        var argc = _testMethodArguments?.Length ?? -1;
        info.AddValue("argc", argc);
        if (_testMethodArguments is not null)
            for (var i = 0; i < _testMethodArguments.Length; i++)
                info.AddValue($"arg{i}", SerializationHelper.Instance.Serialize(_testMethodArguments[i]));
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        _testMethod = Guard.NotNull("Could not retrieve TestMethod from serialization", info.GetValue<IXunitTestMethod>("tm"));
        _testCaseDisplayName = info.GetValue<string?>("dn");
        _skipReason = info.GetValue<string?>("sr");
        _sourceFilePath = info.GetValue<string?>("sf");
        _sourceLineNumber = info.GetValue<int?>("sl");
        _isDelayEnumerated = info.GetValue<bool>("de");
        _skipTestWithoutData = info.GetValue<bool>("swd");
        var argc = info.GetValue<int>("argc");
        if (argc >= 0)
        {
            _testMethodArguments = new object?[argc];
            for (var i = 0; i < argc; i++)
                _testMethodArguments[i] = SerializationHelper.Instance.Deserialize(info.GetValue<string>($"arg{i}")!);
        }
    }

    // ── Execution ─────────────────────────────────────────────────────────────

    public async ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var assemblyUniqueID = TestCollection.TestAssembly.UniqueID;
        var testCollectionUniqueID = TestCollection.UniqueID;
        var testClassUniqueID = TestClass.UniqueID;
        var testMethodUniqueID = TestMethod.UniqueID;
        var testCaseUniqueID = UniqueID;
        var summary = new RunSummary();

        if (!messageBus.QueueMessage(new TestCaseStarting
        {
            AssemblyUniqueID = assemblyUniqueID,
            Explicit = false,
            SkipReason = _skipReason,
            SourceFilePath = _sourceFilePath,
            SourceLineNumber = _sourceLineNumber,
            TestCaseDisplayName = TestCaseDisplayName,
            TestCaseUniqueID = testCaseUniqueID,
            TestClassMetadataToken = TestClassMetadataToken,
            TestClassName = TestClassName,
            TestClassNamespace = TestMethod.TestClass.Class.Namespace,
            TestClassSimpleName = TestClassSimpleName,
            TestClassUniqueID = testClassUniqueID,
            TestCollectionUniqueID = testCollectionUniqueID,
            TestMethodArity = TestMethodArity,
            TestMethodMetadataToken = TestMethodMetadataToken,
            TestMethodName = TestMethodName,
            TestMethodParameterTypesVSTest = TestMethodParameterTypesVSTest,
            TestMethodReturnTypeVSTest = TestMethodReturnTypeVSTest,
            TestMethodUniqueID = testMethodUniqueID,
            Traits = Traits,
        }))
            cancellationTokenSource.Cancel();

        if (_skipReason is not null)
        {
            summary = SendSkippedTestCase(messageBus, cancellationTokenSource,
                assemblyUniqueID, testCollectionUniqueID, testClassUniqueID, testMethodUniqueID, testCaseUniqueID);
        }
        else if (_isDelayEnumerated)
        {
            summary = await RunDelayEnumerated(messageBus, constructorArguments, cancellationTokenSource,
                assemblyUniqueID, testCollectionUniqueID, testClassUniqueID, testMethodUniqueID, testCaseUniqueID);
        }
        else
        {
            summary = await RunWithArguments(messageBus, constructorArguments, _testMethodArguments, cancellationTokenSource,
                assemblyUniqueID, testCollectionUniqueID, testClassUniqueID, testMethodUniqueID, testCaseUniqueID);
        }

        if (!messageBus.QueueMessage(new TestCaseFinished
        {
            AssemblyUniqueID = assemblyUniqueID,
            ExecutionTime = summary.Time,
            TestCaseUniqueID = testCaseUniqueID,
            TestClassUniqueID = testClassUniqueID,
            TestCollectionUniqueID = testCollectionUniqueID,
            TestMethodUniqueID = testMethodUniqueID,
            TestsFailed = summary.Failed,
            TestsNotRun = 0,
            TestsSkipped = summary.Skipped,
            TestsTotal = summary.Total,
        }))
            cancellationTokenSource.Cancel();

        return summary;
    }

    private RunSummary SendSkippedTestCase(
        IMessageBus messageBus, CancellationTokenSource cts,
        string assemblyId, string collectionId, string? classId, string? methodId, string caseId)
    {
        var testUniqueID = UniqueIDGenerator.ForTest(caseId, 0);
        var now = DateTimeOffset.UtcNow;

        if (!messageBus.QueueMessage(new TestStarting
        {
            AssemblyUniqueID = assemblyId,
            Explicit = false,
            StartTime = now,
            TestCaseUniqueID = caseId,
            TestClassUniqueID = classId,
            TestCollectionUniqueID = collectionId,
            TestDisplayName = TestCaseDisplayName,
            TestMethodUniqueID = methodId,
            TestUniqueID = testUniqueID,
            Timeout = 0,
            Traits = Traits,
        }))
            cts.Cancel();

        if (!messageBus.QueueMessage(new TestSkipped
        {
            AssemblyUniqueID = assemblyId,
            ExecutionTime = 0m,
            FinishTime = now,
            Output = string.Empty,
            Reason = _skipReason!,
            TestCaseUniqueID = caseId,
            TestClassUniqueID = classId,
            TestCollectionUniqueID = collectionId,
            TestMethodUniqueID = methodId,
            TestUniqueID = testUniqueID,
            Warnings = null,
        }))
            cts.Cancel();

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
            TestUniqueID = testUniqueID,
            Warnings = null,
        }))
            cts.Cancel();

        return new RunSummary { Total = 1, Skipped = 1 };
    }

    private async ValueTask<RunSummary> RunDelayEnumerated(
        IMessageBus messageBus, object?[] constructorArguments, CancellationTokenSource cts,
        string assemblyId, string collectionId, string? classId, string? methodId, string caseId)
    {
        var summary = new RunSummary();
        await using var disposalTracker = new DisposalTracker();

        foreach (var dataAttr in TestMethod.DataAttributes)
        {
            var rows = await dataAttr.GetData(TestMethod.Method, disposalTracker);
            foreach (var row in rows)
            {
                var args = row.GetData();
                var rowSummary = await RunWithArguments(messageBus, constructorArguments, args, cts,
                    assemblyId, collectionId, classId, methodId, caseId);
                summary.Total += rowSummary.Total;
                summary.Failed += rowSummary.Failed;
                summary.Skipped += rowSummary.Skipped;
                summary.Time += rowSummary.Time;
            }
        }

        return summary;
    }

    private async ValueTask<RunSummary> RunWithArguments(
        IMessageBus messageBus, object?[] constructorArguments, object?[]? methodArguments,
        CancellationTokenSource cts,
        string assemblyId, string collectionId, string? classId, string? methodId, string caseId)
    {
        var summary = new RunSummary();

        // Instantiate test class using constructor arguments provided by xUnit (resolved fixtures)
        var testClassInstance = constructorArguments.Length == 0
            ? Activator.CreateInstance(TestClass.Class)!
            : Activator.CreateInstance(TestClass.Class, constructorArguments)!;

        using var ctx = Scenario.Acquire();
        TestMethod.Method.Invoke(testClassInstance, methodArguments);
        var steps = Scenario.TestDefinitions.OrderBy(td => td.index).ToList();

        var failed = false;

        for (var i = 0; i < steps.Count; i++)
        {
            var td = steps[i];
            var step = new ScenarioStep(this, i, td.Tale, td.Lambda);
            var testUniqueID = step.UniqueID;
            summary.Total++;

            if (failed)
            {
                summary.Skipped++;
                SendSkippedStep(messageBus, cts, step, testUniqueID,
                    assemblyId, collectionId, classId, methodId, caseId, "Previous step failed");
                continue;
            }

            var start = DateTimeOffset.UtcNow;
            if (!messageBus.QueueMessage(new TestStarting
            {
                AssemblyUniqueID = assemblyId,
                Explicit = false,
                StartTime = start,
                TestCaseUniqueID = caseId,
                TestClassUniqueID = classId,
                TestCollectionUniqueID = collectionId,
                TestDisplayName = step.TestDisplayName,
                TestMethodUniqueID = methodId,
                TestUniqueID = testUniqueID,
                Timeout = 0,
                Traits = step.Traits,
            }))
                cts.Cancel();

            Exception? failure = null;
            var sw = Stopwatch.StartNew();

            try
            {
                switch (td.Lambda)
                {
                    case TaleBody.SynchronousTaleBody sync:
                        sync.Body.Invoke();
                        break;
                    case TaleBody.AsynchronousTaleBody async:
                        await async.Body.Invoke();
                        break;
                    default:
                        throw new NotSupportedException($"Unknown lambda type: {td.Lambda.GetType()}");
                }
            }
            catch (Exception ex)
            {
                failure = ex;
                failed = true;
                summary.Failed++;
            }

            sw.Stop();
            var elapsed = (decimal)sw.Elapsed.TotalSeconds;
            var finish = DateTimeOffset.UtcNow;

            if (failure is null)
            {
                if (!messageBus.QueueMessage(new TestPassed
                {
                    AssemblyUniqueID = assemblyId,
                    ExecutionTime = elapsed,
                    FinishTime = finish,
                    Output = string.Empty,
                    TestCaseUniqueID = caseId,
                    TestClassUniqueID = classId,
                    TestCollectionUniqueID = collectionId,
                    TestMethodUniqueID = methodId,
                    TestUniqueID = testUniqueID,
                    Warnings = null,
                }))
                    cts.Cancel();
            }
            else
            {
                var (types, messages, stackTraces, indices, cause) = ExceptionUtility.ExtractMetadata(failure);
                if (!messageBus.QueueMessage(new TestFailed
                {
                    AssemblyUniqueID = assemblyId,
                    Cause = cause,
                    ExceptionParentIndices = indices,
                    ExceptionTypes = types,
                    ExecutionTime = elapsed,
                    FinishTime = finish,
                    Messages = messages,
                    Output = string.Empty,
                    StackTraces = stackTraces,
                    TestCaseUniqueID = caseId,
                    TestClassUniqueID = classId,
                    TestCollectionUniqueID = collectionId,
                    TestMethodUniqueID = methodId,
                    TestUniqueID = testUniqueID,
                    Warnings = null,
                }))
                    cts.Cancel();
            }

            summary.Time += elapsed;

            if (!messageBus.QueueMessage(new TestFinished
            {
                AssemblyUniqueID = assemblyId,
                Attachments = EmptyAttachments,
                ExecutionTime = elapsed,
                FinishTime = finish,
                Output = string.Empty,
                TestCaseUniqueID = caseId,
                TestClassUniqueID = classId,
                TestCollectionUniqueID = collectionId,
                TestMethodUniqueID = methodId,
                TestUniqueID = testUniqueID,
                Warnings = null,
            }))
                cts.Cancel();
        }

        return summary;
    }

    private static void SendSkippedStep(
        IMessageBus messageBus, CancellationTokenSource cts,
        ScenarioStep step, string testUniqueID,
        string assemblyId, string collectionId, string? classId, string? methodId, string caseId,
        string reason)
    {
        var now = DateTimeOffset.UtcNow;

        if (!messageBus.QueueMessage(new TestStarting
        {
            AssemblyUniqueID = assemblyId,
            Explicit = false,
            StartTime = now,
            TestCaseUniqueID = caseId,
            TestClassUniqueID = classId,
            TestCollectionUniqueID = collectionId,
            TestDisplayName = step.TestDisplayName,
            TestMethodUniqueID = methodId,
            TestUniqueID = testUniqueID,
            Timeout = 0,
            Traits = step.Traits,
        }))
            cts.Cancel();

        if (!messageBus.QueueMessage(new TestSkipped
        {
            AssemblyUniqueID = assemblyId,
            ExecutionTime = 0m,
            FinishTime = now,
            Output = string.Empty,
            Reason = reason,
            TestCaseUniqueID = caseId,
            TestClassUniqueID = classId,
            TestCollectionUniqueID = collectionId,
            TestMethodUniqueID = methodId,
            TestUniqueID = testUniqueID,
            Warnings = null,
        }))
            cts.Cancel();

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
            TestUniqueID = testUniqueID,
            Warnings = null,
        }))
            cts.Cancel();
    }
}
