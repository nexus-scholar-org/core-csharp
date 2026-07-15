using NexusScholar.Kernel;
using NexusScholar.Provenance;

namespace NexusScholar.WorkflowExecution.Provenance;

public static class WorkflowExecutionProvenanceProjector
{
    public static ResearchEvent Project(
        WorkflowExecutionJournal journal,
        WorkflowExecutionEvent item)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(item);
        var header = journal.Header;
        if (!string.Equals(header.ExecutionId, item.ExecutionId, StringComparison.Ordinal) ||
            !string.Equals(header.WorkflowId, item.WorkflowId, StringComparison.Ordinal) ||
            header.WorkflowDigest != item.WorkflowDigest ||
            !string.Equals(header.ProtocolVersionId, item.ProtocolVersionId, StringComparison.Ordinal) ||
            header.ProtocolContentDigest != item.ProtocolContentDigest ||
            !journal.Events.Any(accepted => accepted.Digest == item.Digest))
            throw new WorkflowExecutionRuleException(
                WorkflowExecutionErrorCodes.UnverifiedAuthority,
                "Provenance projection requires an accepted event from the supplied execution journal.");

        var subject = new ProvenanceEntityRef("workflow-node-execution", $"{item.ExecutionId}:{item.NodeId}");
        var inputs = new List<ProvenanceEntityRef>
        {
            new("workflow-execution-header", header.ExecutionId, header.Digest),
            new("workflow-execution-authority-policy", header.AuthorityPolicyId, header.AuthorityPolicyDigest),
            new("workflow-execution-chain-head", $"{item.ExecutionId}:{item.Ordinal - 1}", item.PreviousDigest)
        };
        inputs.AddRange(item.Inputs.Select(ToEntity));
        inputs.AddRange(item.Approvals.Select(approval => ToEntity(approval.Record)));
        Add(inputs, item.Decision);
        Add(inputs, item.InvalidationSource);
        Add(inputs, item.SuccessorExecution);

        var outputs = item.Outputs.Select(ToEntity).ToList();
        outputs.Add(new ProvenanceEntityRef("workflow-execution-event", item.EventId, item.Digest));

        return ResearchEventFactory.Create(
            new DigestIdGenerator(item.Digest),
            new FixedClock(item.OccurredAt),
            new ProvenanceActivity(
                $"workflow-execution-{EventToken(item.Kind)}",
                $"Workflow execution {EventToken(item.Kind)}",
                RequiresActor: true,
                RequiresInput: true,
                RequiresOutput: true),
            subject,
            new ProvenanceAgent(item.Actor.ActorId, item.Actor.Kind),
            inputs,
            outputs,
            new ProvenanceProtocolBinding(
                header.ProtocolId,
                header.ProtocolVersionId,
                header.ProtocolVersionNumber,
                header.ProtocolContentDigest),
            new ProvenanceWorkflowBinding(header.WorkflowId, header.WorkflowDigest, item.NodeId));
    }

    private static ProvenanceEntityRef ToEntity(WorkflowExecutionRecordRef value) =>
        new(value.Kind, value.Id, value.Digest);

    private static void Add(ICollection<ProvenanceEntityRef> values, WorkflowExecutionRecordRef? value)
    {
        if (value is not null) values.Add(ToEntity(value));
    }

    private static string EventToken(WorkflowExecutionEventKind value) => value switch
    {
        WorkflowExecutionEventKind.DependenciesSatisfied => "dependencies-satisfied",
        WorkflowExecutionEventKind.WorkStarted => "work-started",
        WorkflowExecutionEventKind.WorkBlocked => "work-blocked",
        WorkflowExecutionEventKind.BlockCleared => "block-cleared",
        WorkflowExecutionEventKind.WorkCompleted => "work-completed",
        WorkflowExecutionEventKind.WorkFailed => "work-failed",
        WorkflowExecutionEventKind.RetryAuthorized => "retry-authorized",
        WorkflowExecutionEventKind.WorkInvalidated => "work-invalidated",
        WorkflowExecutionEventKind.SuccessorBound => "successor-bound",
        _ => throw new WorkflowExecutionRuleException(WorkflowExecutionErrorCodes.InvalidTransition, "Unknown execution event kind.")
    };

    private sealed class FixedClock(DateTimeOffset value) : IClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }

    private sealed class DigestIdGenerator : IIdGenerator
    {
        private readonly Guid _value;

        public DigestIdGenerator(ContentDigest digest)
        {
            var hex = digest.Value[..32];
            _value = Guid.ParseExact($"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..32]}", "D");
        }

        public Guid NewId() => _value;
    }
}
