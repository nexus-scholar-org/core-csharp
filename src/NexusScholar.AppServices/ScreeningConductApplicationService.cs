using NexusScholar.Kernel;
using NexusScholar.Screening;

namespace NexusScholar.AppServices;

public sealed record ScreeningConductChange(
    ScreeningConductPolicy Policy,
    ScreeningConductHeader Header,
    IReadOnlyList<IScreeningConductEntry> CurrentEntries,
    IReadOnlyList<IScreeningConductEntry> ProposedEntries);

public sealed record ScreeningConductPreview(
    string ConductId,
    ContentDigest PriorHeadDigest,
    ContentDigest ResultingHeadDigest,
    int PriorEntryCount,
    int ResultingEntryCount,
    IReadOnlyDictionary<string, ScreeningConductOutcome> Outcomes,
    IReadOnlyList<ScreeningConductConflict> Conflicts,
    bool HandoffReady);

public sealed record ScreeningConductCommitResult(
    string ConductId,
    ContentDigest HeadDigest,
    int EntryCount,
    bool AlreadyApplied);

public interface IScreeningConductCommitPort
{
    ScreeningConductCommitResult Commit(
        ScreeningConductPolicy policy,
        ScreeningConductHeader header,
        IReadOnlyList<IScreeningConductEntry> entries);
}

public static class ScreeningConductApplicationService
{
    public static ScreeningConductPreview Preview(ScreeningConductChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        var current = ScreeningConductJournal.RehydrateEntries(change.Header, change.Policy, change.CurrentEntries);
        var combined = change.CurrentEntries.Concat(change.ProposedEntries).ToArray();
        var resulting = ScreeningConductJournal.RehydrateEntries(change.Header, change.Policy, combined);
        return new ScreeningConductPreview(
            change.Header.ConductId, current.Projection.HeadDigest, resulting.Projection.HeadDigest,
            change.CurrentEntries.Count, combined.Length, resulting.Projection.Outcomes,
            resulting.Projection.Conflicts, resulting.Projection.HandoffReady);
    }

    public static ScreeningConductCommitResult Commit(ScreeningConductChange change, IScreeningConductCommitPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        var preview = Preview(change);
        var entries = change.CurrentEntries.Concat(change.ProposedEntries).ToArray();
        var result = port.Commit(change.Policy, change.Header, entries);
        if (result.ConductId != preview.ConductId || result.HeadDigest != preview.ResultingHeadDigest || result.EntryCount != preview.ResultingEntryCount)
            throw new InvalidOperationException("Screening conduct commit result does not match the validated preview.");
        return result;
    }
}
