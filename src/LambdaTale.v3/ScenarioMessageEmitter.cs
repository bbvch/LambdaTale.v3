using System.Collections.Frozen;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

internal readonly record struct MsgIds(
    string AssemblyId,
    string CollectionId,
    string? ClassId,
    string? MethodId,
    string CaseId);

internal abstract record StepOutcome
{
    public sealed record Passed : StepOutcome;

    public sealed record Skipped(string Reason) : StepOutcome;

    public sealed record Failed(Exception Exception) : StepOutcome;
}

internal sealed class ScenarioMessageEmitter(
    IMessageBus messageBus,
    CancellationTokenSource cts,
    MsgIds ids,
    bool isExplicit,
    int timeout,
    IReadOnlyDictionary<string, IReadOnlyCollection<string>> caseTraits)
{
    public IMessageBus MessageBus => messageBus;
    public CancellationToken CancellationToken => cts.Token;

    public string TestUniqueId(int stepIndex) => UniqueIDGenerator.ForTest(ids.CaseId, stepIndex);

    public ValueTask Queue(IMessageSinkMessage message) => QueueOrCancel(messageBus, message, cts);

    public ValueTask EmitStarting(
        string testUniqueId,
        string displayName,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> traits,
        DateTimeOffset startTime) =>
        QueueOrCancel(messageBus, new TestStarting
        {
            AssemblyUniqueID = ids.AssemblyId,
            Explicit = isExplicit,
            StartTime = startTime,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestDisplayName = displayName,
            TestLabel = null,
            TestMethodUniqueID = ids.MethodId,
            TestUniqueID = testUniqueId,
            Timeout = timeout,
            Traits = traits,
        }, cts);

    public async ValueTask EmitOutcome(
        string testUniqueId,
        DateTimeOffset finishTime,
        decimal elapsed,
        StepOutcome outcome,
        string output)
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
                Output = output,
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
                Output = output,
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
                Output = output,
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
            Output = output,
            TestCaseUniqueID = ids.CaseId,
            TestClassUniqueID = ids.ClassId,
            TestCollectionUniqueID = ids.CollectionId,
            TestMethodUniqueID = ids.MethodId,
            TestUniqueID = testUniqueId,
            Warnings = null,
        }, cts);
    }

    public async ValueTask EmitSynthetic(
        string testUniqueId,
        string displayName,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> traits,
        decimal elapsed,
        StepOutcome outcome)
    {
        var now = DateTimeOffset.UtcNow;
        await this.EmitStarting(testUniqueId, displayName, traits, now);
        await this.EmitOutcome(testUniqueId, now, elapsed, outcome, string.Empty);
    }

    public ValueTask ReportSyntheticFailure(string displayName, int stepIndex, Exception failure, decimal elapsed) =>
        this.EmitSynthetic(this.TestUniqueId(stepIndex), displayName, caseTraits, elapsed, new StepOutcome.Failed(failure));

    // Queues a message and cancels the run if the bus signals it should stop.
    private static async ValueTask QueueOrCancel(IMessageBus messageBus, IMessageSinkMessage message, CancellationTokenSource cts)
    {
        if (!messageBus.QueueMessage(message))
        {
            await cts.CancelAsync();
        }
    }
}
