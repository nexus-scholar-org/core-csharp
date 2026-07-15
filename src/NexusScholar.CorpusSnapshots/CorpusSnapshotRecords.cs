using System;
using System.Linq;
using NexusScholar.Kernel;

namespace NexusScholar.CorpusSnapshots;

public static class CorpusSnapshotConstants
{
    public const string SchemaId = "nexus.corpus.snapshot";
    public const string SchemaVersion = "1.0.0";
    public const string ContentDigestScope = "canonical-json-record";
    public const string RecordDigestScope = "canonical-json-record";
}

public static class CorpusSnapshotErrorCodes
{
    public const string InvalidSnapshot = "invalid-corpus-snapshot";
    public const string NonCanonicalSnapshot = "non-canonical-corpus-snapshot";
    public const string DuplicateSnapshotMaterial = "duplicate-corpus-snapshot-material";
    public const string StaleSourceBinding = "stale-corpus-snapshot-source-binding";
    public const string UnauthorizedPublisher = "unauthorized-corpus-snapshot-publisher";
}

public sealed class CorpusSnapshotAuthorityException : InvalidOperationException
{
    public CorpusSnapshotAuthorityException(string category, string message) : base(message)
    {
        Category = category;
    }

    public string Category { get; }
}

public sealed record CorpusSnapshotDecisionReference(
    string DecisionId,
    ContentDigest DecisionDigest);

public sealed record CorpusSnapshotEvidenceReference(
    string Kind,
    string EvidenceId,
    string DigestScope,
    ContentDigest Digest);

public sealed record CorpusSnapshotGroup(
    string GroupId,
    string RepresentativeCandidateId,
    IReadOnlyList<string> MemberCandidateIds,
    IReadOnlyList<CorpusSnapshotEvidenceReference> EvidenceReferences);

public sealed record CorpusSnapshotUnresolvedCandidate(
    string CandidateId,
    string UnresolvedReason,
    IReadOnlyList<string> RawSightingReferences,
    ContentDigest CandidateContentDigest);

public sealed record CorpusSnapshotInvalidationReference(
    string RecordKind,
    string RecordId,
    ContentDigest RecordDigest);

public sealed record UnverifiedCorpusSnapshot(
    string SchemaId,
    string SchemaVersion,
    string SnapshotId,
    string SourceResultId,
    ContentDigest SourceResultDigest,
    IReadOnlyList<CorpusSnapshotDecisionReference> DecisionReferences,
    ContentDigest DecisionSetDigest,
    IReadOnlyList<CorpusSnapshotGroup> Groups,
    IReadOnlyList<CorpusSnapshotUnresolvedCandidate> UnresolvedCandidates,
    string CreatedByActorId,
    string CreatedByRole,
    string AuthoritySourceId,
    ContentDigest AuthoritySourceDigest,
    DateTimeOffset CreatedAt,
    string? SupersedesSnapshotId,
    ContentDigest? SupersedesSnapshotRecordDigest,
    IReadOnlyList<CorpusSnapshotInvalidationReference> InvalidationReferences,
    ContentDigest? ContentDigest = null,
    ContentDigest? RecordDigest = null);

public sealed class VerifiedCorpusSnapshot
{
    internal VerifiedCorpusSnapshot(
        string schemaId,
        string schemaVersion,
        string snapshotId,
        string sourceResultId,
        ContentDigest sourceResultDigest,
        IReadOnlyList<CorpusSnapshotDecisionReference> decisionReferences,
        ContentDigest decisionSetDigest,
        IReadOnlyList<CorpusSnapshotGroup> groups,
        IReadOnlyList<CorpusSnapshotUnresolvedCandidate> unresolvedCandidates,
        string createdByActorId,
        string createdByRole,
        string authoritySourceId,
        ContentDigest authoritySourceDigest,
        DateTimeOffset createdAt,
        string? supersedesSnapshotId,
        ContentDigest? supersedesSnapshotRecordDigest,
        IReadOnlyList<CorpusSnapshotInvalidationReference> invalidationReferences,
        ContentDigest contentDigest,
        ContentDigest recordDigest,
        DigestEnvelope contentDigestEnvelope,
        DigestEnvelope recordDigestEnvelope)
    {
        SchemaId = schemaId;
        SchemaVersion = schemaVersion;
        SnapshotId = snapshotId;
        SourceResultId = sourceResultId;
        SourceResultDigest = sourceResultDigest;
        DecisionReferences = Array.AsReadOnly(decisionReferences.Select(item => item with { }).ToArray());
        DecisionSetDigest = decisionSetDigest;
        Groups = Array.AsReadOnly(groups.Select(item => item with
        {
            MemberCandidateIds = Array.AsReadOnly(item.MemberCandidateIds.ToArray()),
            EvidenceReferences = Array.AsReadOnly(item.EvidenceReferences.Select(reference => reference with { }).ToArray())
        }).ToArray());
        UnresolvedCandidates = Array.AsReadOnly(unresolvedCandidates.Select(item => item with
        {
            RawSightingReferences = Array.AsReadOnly(item.RawSightingReferences.ToArray())
        }).ToArray());
        CreatedByActorId = createdByActorId;
        CreatedByRole = createdByRole;
        AuthoritySourceId = authoritySourceId;
        AuthoritySourceDigest = authoritySourceDigest;
        CreatedAt = createdAt;
        SupersedesSnapshotId = supersedesSnapshotId;
        SupersedesSnapshotRecordDigest = supersedesSnapshotRecordDigest;
        InvalidationReferences = Array.AsReadOnly(invalidationReferences.Select(item => item with { }).ToArray());
        ContentDigest = contentDigest;
        RecordDigest = recordDigest;
        ContentDigestEnvelope = contentDigestEnvelope;
        RecordDigestEnvelope = recordDigestEnvelope;
    }

    public string SchemaId { get; }
    public string SchemaVersion { get; }
    public string SnapshotId { get; }
    public string SourceResultId { get; }
    public ContentDigest SourceResultDigest { get; }
    public IReadOnlyList<CorpusSnapshotDecisionReference> DecisionReferences { get; }
    public ContentDigest DecisionSetDigest { get; }
    public IReadOnlyList<CorpusSnapshotGroup> Groups { get; }
    public IReadOnlyList<CorpusSnapshotUnresolvedCandidate> UnresolvedCandidates { get; }
    public string CreatedByActorId { get; }
    public string CreatedByRole { get; }
    public string AuthoritySourceId { get; }
    public ContentDigest AuthoritySourceDigest { get; }
    public DateTimeOffset CreatedAt { get; }
    public string? SupersedesSnapshotId { get; }
    public ContentDigest? SupersedesSnapshotRecordDigest { get; }
    public IReadOnlyList<CorpusSnapshotInvalidationReference> InvalidationReferences { get; }
    public ContentDigest ContentDigest { get; }
    public ContentDigest RecordDigest { get; }
    public DigestEnvelope ContentDigestEnvelope { get; }
    public DigestEnvelope RecordDigestEnvelope { get; }
}
