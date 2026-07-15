using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;

namespace NexusScholar.Core.Tests;

[TestClass]
public sealed class CorpusSnapshotTests
{
    [TestMethod]
    public void Create_baseline_deduplicates_groups_and_canonicalizes_baseline_content()
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);
        var first = CorpusSnapshotService.CreateBaseline(
            "snapshot-1",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);
        var second = CorpusSnapshotService.CreateBaseline(
            "snapshot-1",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);

        var expectedGroupIds = new[]
        {
            BuildGroupId("candidate-a", "candidate-b"),
            BuildGroupId("candidate-c"),
            BuildGroupId("candidate-d")
        }.OrderBy(item => item, StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(expectedGroupIds, first.Groups.Select(item => item.GroupId).OrderBy(item => item, StringComparer.Ordinal).ToArray());
        CollectionAssert.AreEqual(expectedGroupIds, second.Groups.Select(item => item.GroupId).OrderBy(item => item, StringComparer.Ordinal).ToArray());
        Assert.AreEqual(Clock.UtcNow, first.CreatedAt);
        Assert.AreEqual(policy.IssuedByActorId, first.CreatedByActorId);
        Assert.AreEqual(policy.IssuedByRole, first.CreatedByRole);
        Assert.AreEqual(0, first.DecisionReferences.Count);

        var expectedDecisionDigest = ComputeDecisionSetDigest(Array.Empty<CorpusSnapshotDecisionReference>());
        Assert.AreEqual(expectedDecisionDigest, first.DecisionSetDigest);

        var groupedById = first.Groups.OrderBy(group => group.GroupId, StringComparer.Ordinal).ToArray();
        Assert.IsTrue(GroupContains(groupedById[0], "candidate-a", "candidate-b"));
        Assert.AreEqual(BuildGroupId("candidate-c"), groupedById[1].GroupId);
        CollectionAssert.AreEqual(new[] { "candidate-c" }, groupedById[1].MemberCandidateIds.ToArray());
        Assert.AreEqual(BuildGroupId("candidate-d"), groupedById[2].GroupId);
        CollectionAssert.AreEqual(new[] { "candidate-d" }, groupedById[2].MemberCandidateIds.ToArray());
    }

    [TestMethod]
    public void Rehydrate_baseline_reproduces_content_and_record_digests()
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);
        var baseline = CorpusSnapshotService.CreateBaseline(
            "snapshot-2",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);

        var persisted = ToUnverifiedSnapshot(baseline);
        var rehydrated = CorpusSnapshotService.Rehydrate(persisted, sourceResult, policy);

        Assert.AreEqual(baseline.ContentDigest, rehydrated.ContentDigest);
        Assert.AreEqual(baseline.RecordDigest, rehydrated.RecordDigest);
    }

    [TestMethod]
    public void Content_digest_remains_same_while_record_digest_changes_with_snapshot_id_and_time()
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);

        var createdEarlier = CorpusSnapshotService.CreateBaseline(
            "snapshot-older",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            EarlierClock);
        var createdLater = CorpusSnapshotService.CreateBaseline(
            "snapshot-later",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            LaterClock);

        Assert.AreEqual(createdEarlier.ContentDigest, createdLater.ContentDigest);
        Assert.AreNotEqual(createdEarlier.RecordDigest, createdLater.RecordDigest);
    }

    [TestMethod]
    public void Create_baseline_rejects_unauthorized_publisher()
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.CreateBaseline(
                "snapshot-unauthorized",
                sourceResult,
                policy,
                "intruder",
                policy.IssuedByRole,
                Clock));

        Assert.AreEqual(CorpusSnapshotErrorCodes.UnauthorizedPublisher, error.Category);
    }

    [TestMethod]
    public void Rehydrate_rejects_wrong_schema_source_policy_and_tampered_dual_digests()
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);
        var baseline = CorpusSnapshotService.CreateBaseline(
            "snapshot-3",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);

        var persisted = ToUnverifiedSnapshot(baseline);

        var wrongSchema = persisted with
        {
            SchemaId = "wrong.schema",
            SchemaVersion = "0.0.0"
        };
        var wrongSource = persisted with
        {
            SourceResultId = $"{baseline.SourceResultId}-wrong"
        };
        var wrongPolicy = persisted with
        {
            AuthoritySourceId = $"{baseline.AuthoritySourceId}-wrong"
        };
        var wrongSourceDigest = persisted with
        {
            SourceResultDigest = ContentDigest.Sha256Utf8("wrong-source-digest")
        };
        var wrongPolicyDigest = persisted with
        {
            AuthoritySourceDigest = ContentDigest.Sha256Utf8("wrong-policy-digest")
        };
        var tamperedContentDigest = persisted with
        {
            ContentDigest = ContentDigest.Sha256Utf8("tampered-content")
        };
        var tamperedRecordDigest = persisted with
        {
            RecordDigest = ContentDigest.Sha256Utf8("tampered-record")
        };

        var wrongSchemaError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(wrongSchema, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, wrongSchemaError.Category);

        var wrongSourceError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(wrongSource, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.StaleSourceBinding, wrongSourceError.Category);

        var wrongSourceDigestError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(wrongSourceDigest, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.StaleSourceBinding, wrongSourceDigestError.Category);

        var wrongPolicyError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(wrongPolicy, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.StaleSourceBinding, wrongPolicyError.Category);

        var wrongPolicyDigestError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(wrongPolicyDigest, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.StaleSourceBinding, wrongPolicyDigestError.Category);

        var tamperedContentError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(tamperedContentDigest, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, tamperedContentError.Category);

        var tamperedRecordError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(tamperedRecordDigest, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, tamperedRecordError.Category);
    }

    [TestMethod]
    public void Rehydrate_rejects_duplicate_or_omitted_memberships_and_representatives_outside_group()
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);
        var baseline = CorpusSnapshotService.CreateBaseline(
            "snapshot-4",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);

        var duplicatedMember = ToUnverifiedSnapshot(baseline) with
        {
            Groups = baseline.Groups.Select(group =>
            {
                if (string.Equals(group.GroupId, BuildGroupId("candidate-a", "candidate-b"), StringComparison.Ordinal))
                {
                    return group with
                    {
                        MemberCandidateIds = new[] { "candidate-a", "candidate-a" }
                    };
                }

                return group;
            }).ToArray()
        };
        var omitted = ToUnverifiedSnapshot(baseline) with
        {
            Groups = baseline.Groups
                .Where(group => !group.MemberCandidateIds.Contains("candidate-b", StringComparer.Ordinal)).ToArray()
        };
        var badRepresentative = ToUnverifiedSnapshot(baseline) with
        {
            Groups = baseline.Groups.Select(group =>
            {
                if (string.Equals(group.GroupId, BuildGroupId("candidate-a", "candidate-b"), StringComparison.Ordinal))
                {
                    return group with { RepresentativeCandidateId = "candidate-d" };
                }

                return group;
            }).ToArray()
        };

        var duplicatedMemberError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(duplicatedMember, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.DuplicateSnapshotMaterial, duplicatedMemberError.Category);

        var omittedError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(omitted, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, omittedError.Category);

        var badRepresentativeError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(badRepresentative, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, badRepresentativeError.Category);
    }

    [TestMethod]
    public void Rehydrate_rejects_wrong_group_ids_and_mismatched_group_evidence()
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);
        var baseline = CorpusSnapshotService.CreateBaseline(
            "snapshot-5",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);

        var wrongGroupId = ToUnverifiedSnapshot(baseline) with
        {
            Groups = baseline.Groups.Select(group =>
            {
                if (string.Equals(group.GroupId, BuildGroupId("candidate-c"), StringComparison.Ordinal))
                {
                    return group with { GroupId = "group-incorrect-id" };
                }

                return group;
            }).ToArray()
        };

        var wrongGroupIdError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(wrongGroupId, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, wrongGroupIdError.Category);

        var wrongEvidence = ToUnverifiedSnapshot(baseline) with
        {
            Groups = baseline.Groups.Select(group =>
            {
                if (group.EvidenceReferences.Count == 0)
                {
                    return group;
                }

                var updatedEvidence = group.EvidenceReferences.Select(reference =>
                {
                    if (string.Equals(reference.EvidenceId, group.EvidenceReferences[0].EvidenceId, StringComparison.Ordinal))
                    {
                        return reference with { Digest = ContentDigest.Sha256Utf8("mismatched-evidence-digest") };
                    }

                    return reference;
                }).ToArray();
                return group with { EvidenceReferences = updatedEvidence };
            }).ToArray()
        };

        var wrongEvidenceError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(wrongEvidence, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, wrongEvidenceError.Category);
    }

    [TestMethod]
    public void Rehydrate_rejects_non_canonical_collection_order()
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);
        var baseline = CorpusSnapshotService.CreateBaseline(
            "snapshot-6",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);

        var groupsOutOfOrder = ToUnverifiedSnapshot(baseline) with
        {
            Groups = baseline.Groups.Reverse().ToArray()
        };
        var groupsOutOfOrderError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(groupsOutOfOrder, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.NonCanonicalSnapshot, groupsOutOfOrderError.Category);

        var membersOutOfOrder = ToUnverifiedSnapshot(baseline) with
        {
            Groups = baseline.Groups.Select(group =>
            {
                if (string.Equals(group.GroupId, BuildGroupId("candidate-a", "candidate-b"), StringComparison.Ordinal))
                {
                    var reversed = group.MemberCandidateIds.Reverse().ToArray();
                    return group with { MemberCandidateIds = reversed };
                }

                return group;
            }).ToArray()
        };
        var membersOutOfOrderError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(membersOutOfOrder, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.NonCanonicalSnapshot, membersOutOfOrderError.Category);

        var evidenceOutOfOrder = ToUnverifiedSnapshot(baseline) with
        {
            Groups = baseline.Groups.Select(group =>
            {
                if (group.EvidenceReferences.Count <= 1)
                {
                    return group;
                }

                return group with { EvidenceReferences = group.EvidenceReferences.Reverse().ToArray() };
            }).ToArray()
        };
        var evidenceOutOfOrderError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(evidenceOutOfOrder, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.NonCanonicalSnapshot, evidenceOutOfOrderError.Category);

        var unresolvedOutOfOrder = ToUnverifiedSnapshot(baseline) with
        {
            UnresolvedCandidates = baseline.UnresolvedCandidates.Reverse().ToArray()
        };
        var unresolvedOutOfOrderError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(unresolvedOutOfOrder, sourceResult, policy));
        Assert.AreEqual(CorpusSnapshotErrorCodes.NonCanonicalSnapshot, unresolvedOutOfOrderError.Category);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Rehydrate_rejects_invalid_supersession_half_boundaries(bool includeSupersededId)
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);
        var baseline = CorpusSnapshotService.CreateBaseline(
            "snapshot-7",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);

        var mutated = includeSupersededId
            ? ToUnverifiedSnapshot(baseline) with { SupersedesSnapshotId = "other-snapshot" }
            : ToUnverifiedSnapshot(baseline) with { SupersedesSnapshotRecordDigest = baseline.RecordDigest };

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(mutated, sourceResult, policy));

        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, error.Category);
    }

    [TestMethod]
    public void Rehydrate_defensively_copies_nested_snapshot_collections()
    {
        var policy = BuildCanonicalPolicy();
        var sourceResult = BuildCanonicalSourceResult(policy.PolicyId);
        var baseline = CorpusSnapshotService.CreateBaseline(
            "snapshot-8",
            sourceResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);

        var firstGroupMembers = baseline.Groups[0].MemberCandidateIds.ToList();
        var firstGroupEvidence = baseline.Groups[0].EvidenceReferences.ToList();
        var firstUnresolvedRaw = baseline.UnresolvedCandidates[0].RawSightingReferences.ToList();

        var groups = baseline.Groups.Select((group, index) =>
        {
            var members = group.MemberCandidateIds.ToList();
            var evidence = group.EvidenceReferences.ToList();
            if (index == 0)
            {
                firstGroupMembers = members;
                firstGroupEvidence = evidence;
            }

            return new CorpusSnapshotGroup(group.GroupId, group.RepresentativeCandidateId, members, evidence);
        }).ToList();

        var unresolved = baseline.UnresolvedCandidates.Select((candidate, index) =>
        {
            var rawSightings = candidate.RawSightingReferences.ToList();
            if (index == 0)
            {
                firstUnresolvedRaw = rawSightings;
            }

            return new CorpusSnapshotUnresolvedCandidate(
                candidate.CandidateId,
                candidate.UnresolvedReason,
                rawSightings,
                candidate.CandidateContentDigest);
        }).ToList();

        var persisted = ToUnverifiedSnapshot(
            baseline,
            groups: groups,
            unresolvedCandidates: unresolved);

        var rehydrated = CorpusSnapshotService.Rehydrate(persisted, sourceResult, policy);

        var expectedMembers = firstGroupMembers.ToArray();
        var expectedEvidence = firstGroupEvidence.Select(reference => reference.Digest).ToArray();
        var expectedRaw = firstUnresolvedRaw.ToArray();

        firstGroupMembers[0] = "candidate-mutated";
        firstGroupEvidence[0] = firstGroupEvidence[0] with
        {
            Digest = ContentDigest.Sha256Utf8("tampered-evidence-copy")
        };
        firstUnresolvedRaw[0] = "sighting-mutated";

        var rehydratedGroup = rehydrated.Groups.Single(item => item.GroupId == baseline.Groups[0].GroupId);
        CollectionAssert.AreEqual(expectedMembers, rehydratedGroup.MemberCandidateIds.ToArray());
        CollectionAssert.AreEqual(expectedEvidence, rehydratedGroup.EvidenceReferences.Select(item => item.Digest).ToArray());
        var rawCandidate = rehydrated.UnresolvedCandidates.Single(item => item.CandidateId == baseline.UnresolvedCandidates[0].CandidateId);
        CollectionAssert.AreEqual(expectedRaw, rawCandidate.RawSightingReferences.ToArray());
    }

    private static bool GroupContains(CorpusSnapshotGroup group, params string[] expected)
    {
        CollectionAssert.AreEquivalent(expected, group.MemberCandidateIds.OrderBy(member => member, StringComparer.Ordinal).ToArray());
        return true;
    }

    private static UnverifiedCorpusSnapshot ToUnverifiedSnapshot(
        VerifiedCorpusSnapshot snapshot,
        IReadOnlyList<CorpusSnapshotGroup>? groups = null,
        IReadOnlyList<CorpusSnapshotUnresolvedCandidate>? unresolvedCandidates = null,
        string? sourceResultId = null,
        ContentDigest? sourceResultDigest = null,
        string? authoritySourceId = null,
        ContentDigest? authoritySourceDigest = null,
        string? snapshotId = null,
        string? schemaId = null,
        string? schemaVersion = null,
        DateTimeOffset? createdAt = null,
        string? supersedesSnapshotId = null,
        ContentDigest? supersedesSnapshotRecordDigest = null,
        ContentDigest? contentDigest = null,
        ContentDigest? recordDigest = null)
    {
        return new UnverifiedCorpusSnapshot(
            schemaId ?? CorpusSnapshotConstants.SchemaId,
            schemaVersion ?? CorpusSnapshotConstants.SchemaVersion,
            snapshotId ?? snapshot.SnapshotId,
            sourceResultId ?? snapshot.SourceResultId,
            sourceResultDigest ?? snapshot.SourceResultDigest,
            snapshot.DecisionReferences,
            snapshot.DecisionSetDigest,
            groups ?? snapshot.Groups,
            unresolvedCandidates ?? snapshot.UnresolvedCandidates,
            snapshot.CreatedByActorId,
            snapshot.CreatedByRole,
            authoritySourceId ?? snapshot.AuthoritySourceId,
            authoritySourceDigest ?? snapshot.AuthoritySourceDigest,
            createdAt ?? snapshot.CreatedAt,
            supersedesSnapshotId,
            supersedesSnapshotRecordDigest,
            snapshot.InvalidationReferences,
            contentDigest ?? snapshot.ContentDigest,
            recordDigest ?? snapshot.RecordDigest);
    }

    private static VerifiedDeduplicationAuthorityPolicy BuildCanonicalPolicy(string policyId = DeduplicationService.PolicyId)
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

    private static VerifiedDeduplicationAuthorityResultDigest BuildCanonicalSourceResult(string policyId)
    {
        var stableA = BuildCandidate("candidate-a");
        var stableB = BuildCandidate("candidate-b");
        var stableC = BuildCandidate("candidate-c");
        var stableD = BuildCandidate("candidate-d");
        var unresolvedE = BuildCandidate("candidate-e", hasStableIdentifier: false);
        var unresolvedF = BuildCandidate("candidate-f", hasStableIdentifier: false);

        var source = new DeduplicationResult(
            "dedup-result-canonical",
            DeduplicationAuthorityDigests.ResultSchemaId,
            DeduplicationAuthorityDigests.ResultSchemaVersion,
            policyId,
            DeduplicationService.PolicyVersion,
            0.95d,
            new System.Collections.Generic.Dictionary<string, int>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { stableA, stableB, stableC, stableD, unresolvedE, unresolvedF },
            new[]
            {
                BuildCluster(
                    new[] { stableA, stableB },
                    BuildRepresentative(stableA),
                    new[]
                    {
                        BuildEvidence("evidence-ab-1", stableA.CandidateId, stableB.CandidateId, policyId),
                        BuildEvidence("evidence-ab-2", stableB.CandidateId, stableA.CandidateId, policyId)
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
            new[] { unresolvedE, unresolvedF },
            Array.Empty<DedupReviewCandidate>(),
            Array.Empty<DedupMessage>(),
            Array.Empty<DedupMessage>(),
            Array.Empty<string>());

        return DeduplicationAuthorityDigests.CreateResultDigestMaterial(source);
    }

    private static DedupCluster BuildCluster(
        IReadOnlyList<DedupCandidateRecord> members,
        DedupRepresentativeResult representative,
        IReadOnlyList<DedupEvidence> evidence) =>
        new DedupCluster(
            $"cluster-{members[0].CandidateId}",
            members,
            representative,
            evidence);

    private static DedupRepresentativeResult BuildRepresentative(DedupCandidateRecord candidate) =>
        new(
            candidate.CandidateId,
            candidate.Title,
            candidate.PrimaryWorkId,
            candidate.WorkIds,
            new[] { candidate.Source.SourceSightingId },
            1d,
            Array.Empty<string>());

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

    private static DedupSightingRef BuildSighting(string suffix) =>
        new DedupSightingRef(
            SourceKind: "search",
            SourceTraceId: $"trace-{suffix}",
            SourceSightingId: $"sighting-{suffix}",
            ProviderAlias: "provider",
            SourceDatabaseOrTool: "tool");

    private static ContentDigest ComputeDecisionSetDigest(IReadOnlyList<CorpusSnapshotDecisionReference> decisionRefs)
    {
        var material = new CanonicalJsonObject().Add(
            "decision_references",
            CanonicalJsonValue.Array(decisionRefs.Select(reference => (CanonicalJsonValue)new CanonicalJsonObject()
                .Add("decision_id", reference.DecisionId)
                .Add("decision_digest", reference.DecisionDigest.ToString()))
                .ToArray()));

        return new DigestEnvelope(
            DigestScope.CanonicalJsonRecord,
            CorpusSnapshotConstants.SchemaId,
            CorpusSnapshotConstants.SchemaVersion,
            material).ComputeDigest();
    }

    private static string BuildGroupId(params string[] members)
    {
        var orderedMembers = members.OrderBy(member => member, StringComparer.Ordinal).ToArray();
        var material = new CanonicalJsonObject().Add(
            "member_candidate_ids",
            CanonicalJsonValue.Array(orderedMembers.Select(CanonicalJsonValue.From).ToArray()));
        return $"group-{ContentDigest.Sha256CanonicalJson(material).Value}";
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }

    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero));
    private static readonly IClock EarlierClock = new FixedClock(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
    private static readonly IClock LaterClock = new FixedClock(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero));
}
