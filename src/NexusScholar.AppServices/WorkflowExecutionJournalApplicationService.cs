using NexusScholar.Kernel;
using NexusScholar.Workflow;
using NexusScholar.WorkflowExecution;

namespace NexusScholar.AppServices;

public sealed record WorkflowExecutionJournalChange(
    VerifiedWorkflowDefinition Workflow,
    WorkflowExecutionAuthorityPolicy Policy,
    WorkflowExecutionHeader Header,
    IWorkflowExecutionRecordResolver RecordResolver,
    IReadOnlyList<WorkflowExecutionEvent> CurrentEvents,
    IReadOnlyList<WorkflowExecutionEvent> ProposedEvents);

public sealed record WorkflowExecutionJournalPreview(
    string ExecutionId,
    ContentDigest PriorHeadDigest,
    ContentDigest ResultingHeadDigest,
    int PriorEventCount,
    int ResultingEventCount,
    IReadOnlyDictionary<string, WorkflowExecutionState> ResultingNodeStates);

public sealed record WorkflowExecutionJournalCommitResult(
    string ExecutionId,
    ContentDigest HeadDigest,
    int EventCount,
    bool AlreadyApplied);

public interface IWorkflowExecutionJournalCommitPort
{
    WorkflowExecutionJournalCommitResult Commit(
        VerifiedWorkflowDefinition workflow,
        WorkflowExecutionAuthorityPolicy policy,
        WorkflowExecutionHeader header,
        IReadOnlyList<WorkflowExecutionEvent> events);
}

public static class WorkflowExecutionJournalApplicationService
{
    public static WorkflowExecutionJournalPreview Preview(WorkflowExecutionJournalChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        var current = WorkflowExecutionJournal.Rehydrate(
            change.Header, change.CurrentEvents, change.Workflow, change.Policy, change.RecordResolver);
        var combined = change.CurrentEvents.Concat(change.ProposedEvents).ToArray();
        var resulting = WorkflowExecutionJournal.Rehydrate(
            change.Header, combined, change.Workflow, change.Policy, change.RecordResolver);
        return new WorkflowExecutionJournalPreview(
            change.Header.ExecutionId,
            current.Projection.HeadDigest,
            resulting.Projection.HeadDigest,
            change.CurrentEvents.Count,
            combined.Length,
            resulting.Projection.NodeStates);
    }

    public static WorkflowExecutionJournalCommitResult Commit(
        WorkflowExecutionJournalChange change,
        IWorkflowExecutionJournalCommitPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        var preview = Preview(change);
        var events = change.CurrentEvents.Concat(change.ProposedEvents).ToArray();
        var result = port.Commit(change.Workflow, change.Policy, change.Header, events);
        if (!string.Equals(result.ExecutionId, preview.ExecutionId, StringComparison.Ordinal) ||
            result.HeadDigest != preview.ResultingHeadDigest || result.EventCount != preview.ResultingEventCount)
            throw new InvalidOperationException("Workflow execution commit port returned a result that does not match the validated preview.");
        return result;
    }
}
