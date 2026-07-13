using NexusScholar.Kernel;

namespace NexusScholar.AI;

public enum AiAuthority
{
    ReadOnlySuggestion,
    BoundedTransformation,
    ScientificDecisionProposal,
    ExternalActionProposal
}

public sealed record AiTaskPolicy
{
    private AiTaskPolicy(
        string taskType,
        AiAuthority authority,
        bool humanApprovalRequired,
        bool evidenceRequired,
        bool externalDataTransferAllowed)
    {
        TaskType = taskType;
        Authority = authority;
        HumanApprovalRequired = humanApprovalRequired;
        EvidenceRequired = evidenceRequired;
        ExternalDataTransferAllowed = externalDataTransferAllowed;
    }

    public string TaskType { get; }

    public AiAuthority Authority { get; }

    public bool HumanApprovalRequired { get; }

    public bool EvidenceRequired { get; }

    public bool ExternalDataTransferAllowed { get; }

    public static AiTaskPolicy Create(
        string taskType,
        AiAuthority authority,
        bool humanApprovalRequired,
        bool evidenceRequired,
        bool externalDataTransferAllowed)
    {
        if (authority is AiAuthority.ScientificDecisionProposal or AiAuthority.ExternalActionProposal &&
            !humanApprovalRequired)
        {
            throw new DomainRuleException("High-authority AI tasks require a recorded human approval step.");
        }

        return new AiTaskPolicy(
            Guard.NotBlank(taskType, nameof(taskType)),
            authority,
            humanApprovalRequired,
            evidenceRequired,
            externalDataTransferAllowed);
    }
}
