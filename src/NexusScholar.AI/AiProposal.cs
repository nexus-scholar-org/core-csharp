using System.Collections.ObjectModel;
using NexusScholar.Kernel;

namespace NexusScholar.AI;

public sealed record AiProposal<T>
{
    public AiProposal(
        AiTaskPolicy policy,
        T value,
        IReadOnlyList<ContentDigest> evidence,
        DateTimeOffset createdAt)
    {
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        Value = value;
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Any(item => !item.IsValid))
        {
            throw new DomainRuleException("AI proposal evidence must use valid content digests.");
        }
        if (policy.EvidenceRequired && evidence.Count == 0)
        {
            throw new DomainRuleException("This AI task policy requires proposal evidence.");
        }
        if (createdAt == default || createdAt.Offset != TimeSpan.Zero)
        {
            throw new DomainRuleException("AI proposal creation time must be a non-default UTC timestamp.");
        }

        Evidence = new ReadOnlyCollection<ContentDigest>(evidence.ToArray());
        CreatedAt = createdAt;
    }

    public AiTaskPolicy Policy { get; }

    public string TaskType => Policy.TaskType;

    public T Value { get; }

    public IReadOnlyList<ContentDigest> Evidence { get; }

    public DateTimeOffset CreatedAt { get; }
}
