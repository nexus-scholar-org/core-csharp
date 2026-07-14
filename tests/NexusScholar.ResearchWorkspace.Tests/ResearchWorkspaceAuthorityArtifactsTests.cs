using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;
using NexusScholar.Provenance;

namespace NexusScholar.ResearchWorkspace.Tests;

[TestClass]
public sealed class ResearchWorkspaceAuthorityArtifactsTests
{
    [TestMethod]
    public void Policy_canonical_record_roundtrips_via_deduplication_rehydration()
    {
        var policy = BuildPolicy();
        var canonical = ResearchWorkspaceAuthorityArtifacts.SerializePolicyCanonicalRecord(policy);

        var verified = ResearchWorkspaceAuthorityArtifacts.VerifyPolicyCanonicalRecord(canonical);

        Assert.AreEqual(policy.PolicyDigest, verified.PolicyDigest);
        Assert.AreEqual(policy.PolicyId, verified.PolicyId);
    }

    [TestMethod]
    public void Policy_verification_rejects_tampered_digest()
    {
        var canonical = ResearchWorkspaceAuthorityArtifacts.SerializePolicyCanonicalRecord(BuildPolicy());
        var mutated = ReplaceTopLevelStringValue(canonical, "policy_digest", ContentDigest.Sha256Utf8("policy-tamper").ToString());

        var error = Assert.ThrowsExactly<DeduplicationAuthorityException>(() =>
            ResearchWorkspaceAuthorityArtifacts.VerifyPolicyCanonicalRecord(mutated));

        Assert.AreEqual(DeduplicationAuthorityPolicyErrorCodes.InvalidAuthorityPolicy, error.Category);
    }

    [TestMethod]
    public void Snapshot_canonical_record_roundtrips_with_source_result_and_policy()
    {
        var policy = BuildPolicy(policyId: DeduplicationService.PolicyId);
        var sourceResult = BuildSourceResult(policy.PolicyId);
        var snapshot = CorpusSnapshotService.CreateBaseline("snapshot-1", sourceResult, policy, policy.IssuedByActorId, policy.IssuedByRole, new FixedClock(new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero)));

        var canonical = ResearchWorkspaceAuthorityArtifacts.SerializeSnapshotCanonicalRecord(snapshot);
        var verified = ResearchWorkspaceAuthorityArtifacts.VerifySnapshotCanonicalRecord(canonical, sourceResult, policy);

        Assert.AreEqual(snapshot.RecordDigest, verified.RecordDigest);
        Assert.AreEqual(snapshot.ContentDigest, verified.ContentDigest);
    }

    [TestMethod]
    public void Snapshot_verification_rejects_tampered_record_digest()
    {
        var policy = BuildPolicy(policyId: DeduplicationService.PolicyId);
        var sourceResult = BuildSourceResult(policy.PolicyId);
        var snapshot = CorpusSnapshotService.CreateBaseline("snapshot-2", sourceResult, policy, policy.IssuedByActorId, policy.IssuedByRole, new FixedClock(new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero)));
        var canonical = ResearchWorkspaceAuthorityArtifacts.SerializeSnapshotCanonicalRecord(snapshot);

        var mutated = ReplaceTopLevelStringValue(canonical, "record_digest", ContentDigest.Sha256Utf8("snapshot-tamper").ToString());

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            ResearchWorkspaceAuthorityArtifacts.VerifySnapshotCanonicalRecord(mutated, sourceResult, policy));

        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, error.Category);
    }

    [TestMethod]
    public void Research_event_canonical_record_roundtrips_with_rehydration()
    {
        var record = BuildResearchEvent();
        var canonical = ResearchWorkspaceAuthorityArtifacts.SerializeResearchEventCanonicalRecord(record);

        var verified = ResearchWorkspaceAuthorityArtifacts.VerifyResearchEventCanonicalRecord(canonical);

        Assert.AreEqual(record.EventId, verified.EventId);
        Assert.AreEqual(record.EventDigest, verified.EventDigest);
    }

    [TestMethod]
    public void Research_event_verification_rejects_tampered_event_digest()
    {
        var record = BuildResearchEvent();
        var canonical = ResearchWorkspaceAuthorityArtifacts.SerializeResearchEventCanonicalRecord(record);
        var mutated = ReplaceTopLevelStringValue(canonical, "event_digest", ContentDigest.Sha256Utf8("event-tamper").ToString());

        Assert.ThrowsExactly<ProvenanceRuleException>(() =>
            ResearchWorkspaceAuthorityArtifacts.VerifyResearchEventCanonicalRecord(mutated));
    }

    private static VerifiedDeduplicationAuthorityPolicy BuildPolicy(string policyId = "policy-fe01")
    {
        return DeduplicationAuthorityPolicy.CreatePolicyMaterial(new UnverifiedDeduplicationAuthorityPolicy(
            SchemaId: DeduplicationAuthorityPolicyConstants.SchemaId,
            SchemaVersion: DeduplicationAuthorityPolicyConstants.SchemaVersion,
            AuthoritySourceKind: DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind,
            PolicyId: policyId,
            PolicyVersion: "1.0.0",
            AuthorizedActorRoles: new[]
            {
                new DeduplicationAuthorityPolicyActorRole("alice", "owner", DeduplicationAuthorityPolicyConstants.HumanSubjectKind)
            },
            AllowedActions: DeduplicationAuthorityPolicyConstants.ClosedActions.ToArray(),
            ReasonCodesByAction: new[]
            {
                new DeduplicationAuthorityPolicyReasonGroup(DeduplicationAuthorityPolicyConstants.MergeAction, new[] { "duplicate" }),
                new DeduplicationAuthorityPolicyReasonGroup(DeduplicationAuthorityPolicyConstants.KeepSeparateAction, new[] { "disputed" }),
                new DeduplicationAuthorityPolicyReasonGroup(DeduplicationAuthorityPolicyConstants.MarkUnresolvedAction, new[] { "uncertain" })
            },
            RequiresRationale: false,
            IssuedByActorId: "alice",
            IssuedByRole: "owner",
            IssuedAt: new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero),
            SupersedesPolicyId: null,
            SupersedesPolicyDigest: null,
            PolicyDigest: null));
    }

    private static VerifiedDeduplicationAuthorityResultDigest BuildSourceResult(string policyId)
    {
        var stableA = BuildCandidate("candidate-a");
        var stableB = BuildCandidate("candidate-b");
        var stableC = BuildCandidate("candidate-c");

        var source = new DeduplicationResult(
            "dedup-result-canonical",
            DeduplicationAuthorityDigests.ResultSchemaId,
            DeduplicationAuthorityDigests.ResultSchemaVersion,
            policyId,
            DeduplicationService.PolicyVersion,
            0.95d,
            new Dictionary<string, int>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { stableA, stableB, stableC },
            new[]
            {
                BuildCluster(
                    new[] { stableA, stableB },
                    BuildRepresentative(stableA),
                    new[]
                    {
                        BuildEvidence("evidence-ab", stableA.CandidateId, stableB.CandidateId, policyId)
                    }),
                BuildCluster(
                    new[] { stableC },
                    BuildRepresentative(stableC),
                    new[]
                    {
                        BuildEvidence("evidence-c", stableC.CandidateId, stableC.CandidateId, policyId)
                    })
            },
            Array.Empty<DedupEvidence>(),
            Array.Empty<DedupCandidateRecord>(),
            Array.Empty<DedupReviewCandidate>(),
            Array.Empty<DedupMessage>(),
            Array.Empty<DedupMessage>(),
            Array.Empty<string>());

        return DeduplicationAuthorityDigests.CreateResultDigestMaterial(source);
    }

    private static ResearchEvent BuildResearchEvent()
    {
        var ids = new FixedGuidIdGenerator(
            Guid.Parse("00000000-0000-0000-0000-000000000001", CultureInfo.InvariantCulture),
            Guid.Parse("00000000-0000-0000-0000-000000000002", CultureInfo.InvariantCulture),
            Guid.Parse("00000000-0000-0000-0000-000000000003", CultureInfo.InvariantCulture),
            Guid.Parse("00000000-0000-0000-0000-000000000004", CultureInfo.InvariantCulture));
        var clock = new FixedClock(new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero));
        var activity = new ProvenanceActivity(
            "corpus-snapshot-published",
            "Corpus snapshot published",
            RequiresActor: true,
            RequiresInput: true,
            RequiresOutput: true);

        var snapshotRef = new ProvenanceEntityRef("nexus.corpus.snapshot", "snapshot-1", ContentDigest.Sha256Utf8("snapshot"));
        var sourceResultRef = new ProvenanceEntityRef("nexus.deduplication.result", "result-1", ContentDigest.Sha256Utf8("result"));
        var policyRef = new ProvenanceEntityRef("local-deduplication-authority-policy", "policy-1", ContentDigest.Sha256Utf8("policy"));
        var decisionSetRef = new ProvenanceEntityRef("deduplication-decision-set", "set-1", ContentDigest.Sha256Utf8("set"));

        return ResearchEventFactory.Create(
            ids,
            clock,
            activity,
            snapshotRef,
            new ProvenanceAgent("alice", ProvenanceAgent.HumanKind),
            inputs: new[] { sourceResultRef, policyRef, decisionSetRef },
            outputs: new[] { snapshotRef });
    }

    private static byte[] ReplaceTopLevelStringValue(byte[] canonical, string propertyName, string replacement)
    {
        var root = JsonDocument.Parse(canonical).RootElement;
        var values = CanonicalJsonValue.FromJsonElement(root);
        if (values is not CanonicalJsonObject rootObject)
        {
            throw new InvalidOperationException("Canonical record root must be an object.");
        }

        var rebuilt = new CanonicalJsonObject();
        foreach (var pair in rootObject.Properties)
        {
            if (string.Equals(pair.Key, propertyName, StringComparison.Ordinal))
            {
                rebuilt.Add(pair.Key, CanonicalJsonValue.From(replacement));
            }
            else
            {
                rebuilt.Add(pair.Key, pair.Value);
            }
        }

        return CanonicalJsonSerializer.SerializeToUtf8Bytes(rebuilt);
    }

    private static DedupCandidateRecord BuildCandidate(string candidateId, bool hasStableIdentifier = true) =>
        new DedupCandidateRecord(
            candidateId,
            $"Title {candidateId}",
            hasStableIdentifier,
            hasStableIdentifier ? $"{candidateId}-doi" : null,
            new[] { "work-id" },
            Array.Empty<string>(),
            BuildSighting(candidateId),
            new[] { "author" },
            2026,
            null,
            null,
            new[] { "keyword" });

    private static DedupRepresentativeResult BuildRepresentative(DedupCandidateRecord candidate) =>
        new(
            candidate.CandidateId,
            candidate.Title,
            candidate.PrimaryWorkId,
            candidate.WorkIds,
            new[] { candidate.Source.SourceSightingId },
            1d,
            Array.Empty<string>());

    private static DedupCluster BuildCluster(
        IReadOnlyList<DedupCandidateRecord> members,
        DedupRepresentativeResult representative,
        IReadOnlyList<DedupEvidence> evidence) =>
        new DedupCluster(
            $"cluster-{members[0].CandidateId}",
            members,
            representative,
            evidence);

    private static DedupEvidence BuildEvidence(
        string evidenceId,
        string subjectCandidateId,
        string objectCandidateId,
        string policyId) =>
        new DedupEvidence(
            evidenceId,
            DedupEvidenceKind.SourceSighting,
            subjectCandidateId,
            objectCandidateId,
            "source-sighting",
            true,
            0.96,
            policyId,
            DeduplicationService.PolicyVersion);

    private static DedupSightingRef BuildSighting(string suffix) =>
        new DedupSightingRef(
            SourceKind: "search",
            SourceTraceId: $"trace-{suffix}",
            SourceSightingId: $"sighting-{suffix}",
            ProviderAlias: "provider",
            SourceDatabaseOrTool: "tool");

    private sealed class FixedGuidIdGenerator(params Guid[] ids) : IIdGenerator
    {
        private readonly Queue<Guid> _ids = new(ids);

        public Guid NewId() => _ids.Count == 0 ? Guid.NewGuid() : _ids.Dequeue();
    }

    private sealed class FixedClock(DateTimeOffset value) : IClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }
}
