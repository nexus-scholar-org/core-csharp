using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;

namespace NexusScholar.Core.Tests;

[TestClass]
public sealed class CorpusSnapshotInvalidationTests
{
    private static readonly IClock BaselineClock = new FixedClock(new DateTimeOffset(2026, 7, 14, 9, 0, 0, TimeSpan.Zero));
    private static readonly IClock DecisionClock = new FixedClock(new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero));
    private static readonly IClock SuccessorClock = new FixedClock(new DateTimeOffset(2026, 7, 14, 11, 0, 0, TimeSpan.Zero));
    private static readonly IClock InvalidationClock = new FixedClock(new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero));

    [TestMethod]
    public void Create_and_rehydrate_resolves_exact_verified_authority_chain()
    {
        var chain = BuildChain();
        var input = BuildInvalidation(chain);

        var created = Create(input, chain);
        var persisted = input with
        {
            InvalidatedAt = created.InvalidatedAt,
            InvalidatedRecordReferences = created.InvalidatedRecordReferences,
            RecordDigest = created.RecordDigest
        };
        var rehydrated = Rehydrate(persisted, chain);

        Assert.AreEqual(created.RecordDigest, rehydrated.RecordDigest);
        Assert.AreEqual(InvalidationClock.UtcNow, rehydrated.InvalidatedAt);
        CollectionAssert.AreEqual(
            new[] { chain.Baseline.SnapshotId },
            rehydrated.InvalidatedRecordReferences.Select(item => item.RecordId).ToArray());
    }

    [TestMethod]
    public void Successor_rehydrate_resolves_predecessor_decisions_and_invalidated_records()
    {
        var chain = BuildChain();
        var persisted = ToUnverified(chain.Successor);

        var reopened = CorpusSnapshotService.RehydrateSuccessor(
            persisted,
            chain.Source,
            chain.Policy,
            chain.Baseline,
            new[] { chain.CauseDecision },
            chain.KnownDecisions,
            chain.KnownSnapshots);

        Assert.AreEqual(chain.Successor.RecordDigest, reopened.RecordDigest);
        Assert.AreEqual(chain.Baseline.RecordDigest, reopened.SupersedesSnapshotRecordDigest);
    }

    [TestMethod]
    public void Successor_rehydrate_rejects_active_decision_set_that_does_not_resolve_persisted_references()
    {
        var chain = BuildChain(includePreviousDecision: true);
        var persisted = ToUnverified(chain.Successor);

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() => CorpusSnapshotService.RehydrateSuccessor(
            persisted,
            chain.Source,
            chain.Policy,
            chain.Baseline,
            new[] { chain.PreviousDecision! },
            chain.KnownDecisions,
            chain.KnownSnapshots));

        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, error.Category);
    }

    [TestMethod]
    public void Successor_rejects_verified_decision_bound_to_a_fabricated_predecessor()
    {
        var chain = BuildChain();
        var target = DeduplicationAuthorityDigests.CreateReviewTargetDigestMaterial(
            chain.Source,
            chain.Source.Result.ReviewRequiredCandidates[0],
            new[] { "candidate-a", "candidate-b" },
            chain.Source.Result.Evidence);
        var fabricated = CreateDecision(
            "decision-fabricated-predecessor",
            null,
            chain.Policy,
            chain.Source,
            target,
            chain.Baseline,
            "snapshot-fabricated",
            ContentDigest.Sha256Utf8("snapshot-fabricated"));

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() => CorpusSnapshotService.CreateSuccessor(
            "snapshot-rejected",
            chain.Baseline,
            chain.Policy,
            "alice",
            "owner",
            SuccessorClock,
            new[] { fabricated },
            new[] { new CorpusSnapshotInvalidationReference(CorpusSnapshotInvalidationConstants.InvalidationSnapshotKind, chain.Baseline.SnapshotId, chain.Baseline.RecordDigest) },
            new[] { fabricated },
            new[] { chain.Baseline }));

        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, error.Category);
    }

    [TestMethod]
    public void Baseline_rehydrate_maps_null_collection_entries_to_stable_domain_error()
    {
        var chain = BuildChain();
        var malformed = ToUnverified(chain.Baseline) with
        {
            DecisionReferences = new CorpusSnapshotDecisionReference[] { null! }
        };

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            CorpusSnapshotService.Rehydrate(malformed, chain.Source, chain.Policy));

        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, error.Category);
    }

    [TestMethod]
    public void Successor_create_maps_null_active_decision_to_stable_domain_error()
    {
        var chain = BuildChain();

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() => CorpusSnapshotService.CreateSuccessor(
            "snapshot-null-decision",
            chain.Baseline,
            chain.Policy,
            "alice",
            "owner",
            SuccessorClock,
            new VerifiedDeduplicationAuthorityDecision[] { null! },
            new[] { new CorpusSnapshotInvalidationReference(CorpusSnapshotInvalidationConstants.InvalidationSnapshotKind, chain.Baseline.SnapshotId, chain.Baseline.RecordDigest) },
            chain.KnownDecisions,
            chain.KnownSnapshots));

        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, error.Category);
    }

    [TestMethod]
    public void Successor_rejects_active_set_containing_a_decision_and_its_replacement()
    {
        var chain = BuildChain(includePreviousDecision: true);

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() => CorpusSnapshotService.CreateSuccessor(
            "snapshot-conflicting-active-set",
            chain.Baseline,
            chain.Policy,
            "alice",
            "owner",
            SuccessorClock,
            new[] { chain.PreviousDecision!, chain.CauseDecision },
            new[]
            {
                new CorpusSnapshotInvalidationReference(
                    CorpusSnapshotInvalidationConstants.InvalidationSnapshotKind,
                    chain.Baseline.SnapshotId,
                    chain.Baseline.RecordDigest)
            },
            chain.KnownDecisions,
            chain.KnownSnapshots));

        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, error.Category);
    }

    [TestMethod]
    public void Create_rejects_fabricated_cause_decision_digest()
    {
        var chain = BuildChain();
        var input = BuildInvalidation(chain) with { CauseDecisionDigest = ContentDigest.Sha256Utf8("fabricated") };

        var error = Assert.ThrowsExactly<CorpusSnapshotInvalidationException>(() => Create(input, chain));

        Assert.AreEqual(CorpusSnapshotInvalidationErrorCodes.InvalidInvalidationRecord, error.Category);
    }

    [TestMethod]
    public void Create_rejects_dangling_invalidated_record()
    {
        var chain = BuildChain();
        var input = BuildInvalidation(chain) with
        {
            InvalidatedRecordReferences = new[]
            {
                new CorpusSnapshotInvalidationInvalidatedRecordReference(
                    CorpusSnapshotInvalidationConstants.InvalidationSnapshotKind,
                    "snapshot-missing",
                    ContentDigest.Sha256Utf8("snapshot-missing"))
            }
        };

        var error = Assert.ThrowsExactly<CorpusSnapshotInvalidationException>(() => Create(input, chain));

        Assert.AreEqual(CorpusSnapshotInvalidationErrorCodes.InvalidInvalidationRecord, error.Category);
    }

    [TestMethod]
    public void Rehydrate_rejects_noncanonical_reference_order()
    {
        var chain = BuildChain(includePreviousDecision: true);
        var input = BuildInvalidation(chain);
        var created = Create(input, chain);
        var persisted = input with
        {
            InvalidatedAt = created.InvalidatedAt,
            InvalidatedRecordReferences = created.InvalidatedRecordReferences.Reverse().ToArray(),
            RecordDigest = created.RecordDigest
        };

        var error = Assert.ThrowsExactly<CorpusSnapshotInvalidationException>(() => Rehydrate(persisted, chain));

        Assert.AreEqual(CorpusSnapshotInvalidationErrorCodes.NonCanonicalInvalidationMaterial, error.Category);
    }

    [TestMethod]
    public void Create_rejects_unauthorized_actor_and_stale_policy_binding()
    {
        var chain = BuildChain();
        var unauthorized = BuildInvalidation(chain) with { ActorId = "automation", ActorRole = "system" };
        var stale = BuildInvalidation(chain) with
        {
            AuthoritySourceId = "policy-other",
            AuthoritySourceDigest = ContentDigest.Sha256Utf8("policy-other")
        };

        var actorError = Assert.ThrowsExactly<CorpusSnapshotInvalidationException>(() => Create(unauthorized, chain));
        var policyError = Assert.ThrowsExactly<CorpusSnapshotInvalidationException>(() => Create(stale, chain));

        Assert.AreEqual(CorpusSnapshotInvalidationErrorCodes.UnauthorizedInvalidationActor, actorError.Category);
        Assert.AreEqual(CorpusSnapshotInvalidationErrorCodes.StaleAuthoritySourceBinding, policyError.Category);
    }

    private static VerifiedCorpusSnapshotInvalidation Create(UnverifiedCorpusSnapshotInvalidation input, AuthorityChain chain) =>
        CorpusSnapshotInvalidation.CreateInvalidationMaterial(
            input,
            InvalidationClock,
            chain.Policy,
            chain.CauseDecision,
            chain.Successor,
            chain.KnownDecisions,
            chain.KnownSnapshots);

    private static VerifiedCorpusSnapshotInvalidation Rehydrate(UnverifiedCorpusSnapshotInvalidation input, AuthorityChain chain) =>
        CorpusSnapshotInvalidation.RehydrateInvalidationMaterial(
            input,
            chain.Policy,
            chain.CauseDecision,
            chain.Successor,
            chain.KnownDecisions,
            chain.KnownSnapshots);

    private static UnverifiedCorpusSnapshotInvalidation BuildInvalidation(AuthorityChain chain)
    {
        var references = new List<CorpusSnapshotInvalidationInvalidatedRecordReference>
        {
            new(
                CorpusSnapshotInvalidationConstants.InvalidationSnapshotKind,
                chain.Baseline.SnapshotId,
                chain.Baseline.RecordDigest)
        };
        if (chain.PreviousDecision is not null)
        {
            references.Add(new(
                CorpusSnapshotInvalidationConstants.InvalidationDecisionKind,
                chain.PreviousDecision.DecisionId,
                chain.PreviousDecision.DecisionDigest));
        }

        return new UnverifiedCorpusSnapshotInvalidation(
            CorpusSnapshotInvalidationConstants.SchemaId,
            CorpusSnapshotInvalidationConstants.SchemaVersion,
            "invalidation-1",
            chain.CauseDecision.DecisionId,
            chain.CauseDecision.DecisionDigest,
            chain.Successor.SnapshotId,
            chain.Successor.RecordDigest,
            references,
            "alice",
            "owner",
            chain.Policy.PolicyId,
            DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind,
            chain.Policy.PolicyDigest,
            InvalidationClock.UtcNow);
    }

    private static AuthorityChain BuildChain(bool includePreviousDecision = false)
    {
        var policy = BuildPolicy();
        var source = BuildSource(policy.PolicyId);
        var baseline = CorpusSnapshotService.CreateBaseline(
            "snapshot-baseline",
            source,
            policy,
            "alice",
            "owner",
            BaselineClock);
        var target = DeduplicationAuthorityDigests.CreateReviewTargetDigestMaterial(
            source,
            source.Result.ReviewRequiredCandidates[0],
            new[] { "candidate-a", "candidate-b" },
            source.Result.Evidence);

        VerifiedDeduplicationAuthorityDecision? previous = null;
        if (includePreviousDecision)
        {
            previous = CreateDecision("decision-previous", null, policy, source, target, baseline);
        }

        var cause = CreateDecision("decision-cause", previous?.DecisionId, policy, source, target, baseline);
        var knownDecisions = previous is null ? new[] { cause } : new[] { cause, previous };
        var invalidatedRecords = new List<CorpusSnapshotInvalidationReference>
        {
            new(CorpusSnapshotInvalidationConstants.InvalidationSnapshotKind, baseline.SnapshotId, baseline.RecordDigest)
        };
        if (previous is not null)
        {
            invalidatedRecords.Add(new(
                CorpusSnapshotInvalidationConstants.InvalidationDecisionKind,
                previous.DecisionId,
                previous.DecisionDigest));
        }

        var successor = CorpusSnapshotService.CreateSuccessor(
            "snapshot-successor",
            baseline,
            policy,
            "alice",
            "owner",
            SuccessorClock,
            new[] { cause },
            invalidatedRecords,
            knownDecisions,
            new[] { baseline });

        return new AuthorityChain(policy, source, baseline, successor, cause, previous, knownDecisions, new[] { baseline, successor });
    }

    private static UnverifiedCorpusSnapshot ToUnverified(VerifiedCorpusSnapshot snapshot) => new(
        snapshot.SchemaId,
        snapshot.SchemaVersion,
        snapshot.SnapshotId,
        snapshot.SourceResultId,
        snapshot.SourceResultDigest,
        snapshot.DecisionReferences,
        snapshot.DecisionSetDigest,
        snapshot.Groups,
        snapshot.UnresolvedCandidates,
        snapshot.CreatedByActorId,
        snapshot.CreatedByRole,
        snapshot.AuthoritySourceId,
        snapshot.AuthoritySourceDigest,
        snapshot.CreatedAt,
        snapshot.SupersedesSnapshotId,
        snapshot.SupersedesSnapshotRecordDigest,
        snapshot.InvalidationReferences,
        snapshot.ContentDigest,
        snapshot.RecordDigest);

    private static VerifiedDeduplicationAuthorityDecision CreateDecision(
        string decisionId,
        string? supersedesDecisionId,
        VerifiedDeduplicationAuthorityPolicy policy,
        VerifiedDeduplicationAuthorityResultDigest source,
        VerifiedDeduplicationAuthorityReviewTargetDigest target,
        VerifiedCorpusSnapshot baseline,
        string? sourceSnapshotId = null,
        ContentDigest? sourceSnapshotDigest = null)
    {
        var evidence = target.Evidence.Select(item => new DeduplicationAuthorityDecisionEvidenceReference(
            item.Kind.ToString(),
            item.EvidenceId,
            DigestScope.CanonicalJsonRecord.Value,
            DeduplicationAuthorityDigests.CreateEvidenceDigestMaterial(item).EvidenceDigest)).ToArray();
        var invalidations = new List<DeduplicationAuthorityDecisionInvalidationEffect>
        {
            new(DeduplicationDecisionConstants.InvalidationSnapshotKind, baseline.SnapshotId, baseline.RecordDigest)
        };

        return DeduplicationDecision.CreateDecisionMaterial(
            new UnverifiedDeduplicationAuthorityDecision(
                DeduplicationDecisionConstants.SchemaId,
                DeduplicationDecisionConstants.SchemaVersion,
                decisionId,
                DeduplicationAuthorityPolicyConstants.MergeAction,
                policy.PolicyId,
                policy.PolicyVersion,
                target.TargetKind,
                target.TargetId,
                target.TargetDigest,
                source.Result.ResultId,
                source.ResultDigest,
                sourceSnapshotId ?? baseline.SnapshotId,
                sourceSnapshotDigest ?? baseline.RecordDigest,
                evidence,
                "alice",
                "owner",
                policy.PolicyId,
                DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind,
                policy.PolicyDigest,
                null,
                "duplicate",
                DecisionClock.UtcNow,
                supersedesDecisionId,
                invalidations),
            DecisionClock,
            policy,
            source,
            target);
    }

    private static VerifiedDeduplicationAuthorityPolicy BuildPolicy() =>
        DeduplicationAuthorityPolicy.CreatePolicyMaterial(new UnverifiedDeduplicationAuthorityPolicy(
            DeduplicationAuthorityPolicyConstants.SchemaId,
            DeduplicationAuthorityPolicyConstants.SchemaVersion,
            DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind,
            DeduplicationService.PolicyId,
            DeduplicationService.PolicyVersion,
            new[] { new DeduplicationAuthorityPolicyActorRole("alice", "owner") },
            DeduplicationAuthorityPolicyConstants.ClosedActions,
            new[]
            {
                new DeduplicationAuthorityPolicyReasonGroup(DeduplicationAuthorityPolicyConstants.MergeAction, new[] { "duplicate" }),
                new DeduplicationAuthorityPolicyReasonGroup(DeduplicationAuthorityPolicyConstants.KeepSeparateAction, new[] { "different" }),
                new DeduplicationAuthorityPolicyReasonGroup(DeduplicationAuthorityPolicyConstants.MarkUnresolvedAction, new[] { "uncertain" })
            },
            false,
            "alice",
            "owner",
            BaselineClock.UtcNow));

    private static VerifiedDeduplicationAuthorityResultDigest BuildSource(string policyId)
    {
        var a = Candidate("candidate-a");
        var b = Candidate("candidate-b");
        var evidence = new[]
        {
            new DedupEvidence("evidence-a", DedupEvidenceKind.SourceSighting, a.CandidateId, b.CandidateId, "match", true, 0.96, policyId, DeduplicationService.PolicyVersion)
        };
        var result = new DeduplicationResult(
            "result-1",
            DeduplicationAuthorityDigests.ResultSchemaId,
            DeduplicationAuthorityDigests.ResultSchemaVersion,
            policyId,
            DeduplicationService.PolicyVersion,
            0.95,
            new Dictionary<string, int>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { a, b },
            Array.Empty<DedupCluster>(),
            evidence,
            Array.Empty<DedupCandidateRecord>(),
            new[] { new DedupReviewCandidate(a.CandidateId, b.CandidateId, 0.96, 0.95) },
            Array.Empty<DedupMessage>(),
            Array.Empty<DedupMessage>(),
            Array.Empty<string>());
        return DeduplicationAuthorityDigests.CreateResultDigestMaterial(result);
    }

    private static DedupCandidateRecord Candidate(string id) => new(
        id,
        $"Title {id}",
        true,
        $"doi-{id}",
        new[] { $"work-{id}" },
        new[] { $"record-{id}" },
        new DedupSightingRef("search", $"trace-{id}", SourceSightingId: $"sighting-{id}", ProviderAlias: "fixture", SourceDatabaseOrTool: "fixture"));

    private sealed record AuthorityChain(
        VerifiedDeduplicationAuthorityPolicy Policy,
        VerifiedDeduplicationAuthorityResultDigest Source,
        VerifiedCorpusSnapshot Baseline,
        VerifiedCorpusSnapshot Successor,
        VerifiedDeduplicationAuthorityDecision CauseDecision,
        VerifiedDeduplicationAuthorityDecision? PreviousDecision,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> KnownDecisions,
        IReadOnlyList<VerifiedCorpusSnapshot> KnownSnapshots);

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
