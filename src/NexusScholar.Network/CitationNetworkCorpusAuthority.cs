using System.Collections.ObjectModel;
using NexusScholar.Kernel;

namespace NexusScholar.Network;

public interface ICitationNetworkCorpusAuthorityResolver
{
    CitationNetworkCorpusAuthority Resolve(string corpusSnapshotId, ContentDigest corpusSnapshotDigest);
}

public sealed class CitationNetworkCorpusAuthority
{
    public CitationNetworkCorpusAuthority(
        string corpusSnapshotId,
        ContentDigest corpusSnapshotDigest,
        IReadOnlyCollection<string> resolvedNodeIds)
    {
        CorpusSnapshotId = Guard.NotBlank(corpusSnapshotId, nameof(corpusSnapshotId));
        if (!corpusSnapshotDigest.IsValid)
        {
            throw new CitationNetworkRuleException(
                CitationNetworkErrorCodes.InvalidCorpusSnapshot,
                "Resolved corpus authority digest must be valid.");
        }

        ArgumentNullException.ThrowIfNull(resolvedNodeIds);
        var normalized = resolvedNodeIds
            .Select(nodeId => Guard.NotBlank(nodeId, nameof(resolvedNodeIds)))
            .OrderBy(nodeId => nodeId, StringComparer.Ordinal)
            .ToArray();
        if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Length)
        {
            throw new CitationNetworkRuleException(
                CitationNetworkErrorCodes.InvalidCorpusSnapshot,
                "Resolved corpus authority node identities must be unique.");
        }

        CorpusSnapshotDigest = corpusSnapshotDigest;
        ResolvedNodeIds = new ReadOnlyCollection<string>(normalized);
    }

    public string CorpusSnapshotId { get; }

    public ContentDigest CorpusSnapshotDigest { get; }

    public ReadOnlyCollection<string> ResolvedNodeIds { get; }
}
