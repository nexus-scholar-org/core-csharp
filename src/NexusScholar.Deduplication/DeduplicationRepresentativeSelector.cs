namespace NexusScholar.Deduplication;

public static class DeduplicationRepresentativeSelector
{
    public static DedupRepresentativeResult Select(IReadOnlyList<DedupCandidateRecord> members) => Elect(members);

    public static DedupRepresentativeResult Elect(IReadOnlyList<DedupCandidateRecord> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        if (members.Count == 0 || members.Any(member => member is null))
        {
            throw new ArgumentException("Representative election requires at least one candidate.", nameof(members));
        }

        return DeduplicationService.ElectRepresentative(members.ToArray());
    }
}
