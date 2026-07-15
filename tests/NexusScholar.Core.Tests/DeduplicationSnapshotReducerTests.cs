using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;

namespace NexusScholar.Core.Tests;

[TestClass]
public sealed class DeduplicationSnapshotReducerTests
{
    [TestMethod]
    public void Merge_unions_groups_and_unresolved_entries_with_canonical_identity_and_evidence()
    {
        var fixture = Fixture.Create();
        var groupMerge = fixture.Apply(fixture.Baseline, [], "a", "b", DeduplicationAuthorityPolicyConstants.MergeAction);

        CollectionAssert.AreEqual(new[] { "a", "b" }, groupMerge.Snapshot.Groups.Single(group => group.MemberCandidateIds.Contains("a")).MemberCandidateIds.ToArray());
        Assert.AreEqual(BuildGroupId("a", "b"), groupMerge.Snapshot.Groups.Single(group => group.MemberCandidateIds.Contains("a")).GroupId);
        Assert.AreEqual(1, groupMerge.Snapshot.Groups.Single(group => group.MemberCandidateIds.Contains("a")).EvidenceReferences.Count);

        var groupAndUnresolved = fixture.Apply(fixture.Baseline, [], "a", "x", DeduplicationAuthorityPolicyConstants.MergeAction);
        CollectionAssert.AreEqual(new[] { "a", "x" }, groupAndUnresolved.Snapshot.Groups.Single(group => group.MemberCandidateIds.Contains("x")).MemberCandidateIds.ToArray());
        Assert.IsFalse(groupAndUnresolved.Snapshot.UnresolvedCandidates.Any(candidate => candidate.CandidateId == "x"));
        Assert.IsTrue(groupAndUnresolved.Snapshot.UnresolvedCandidates.Any(candidate => candidate.CandidateId == "y"));

        var unresolvedMerge = fixture.Apply(fixture.Baseline, [], "x", "y", DeduplicationAuthorityPolicyConstants.MergeAction);
        CollectionAssert.AreEqual(new[] { "x", "y" }, unresolvedMerge.Snapshot.Groups.Single(group => group.MemberCandidateIds.Contains("x")).MemberCandidateIds.ToArray());
        Assert.AreEqual(0, unresolvedMerge.Snapshot.UnresolvedCandidates.Count);
    }

    [TestMethod]
    public void Keep_separate_and_mark_unresolved_preserve_membership_and_project_active_state_from_references()
    {
        var fixture = Fixture.Create();
        var kept = fixture.Apply(fixture.Baseline, [], "a", "b", DeduplicationAuthorityPolicyConstants.KeepSeparateAction);
        CollectionAssert.AreEqual(fixture.Baseline.Groups.Select(group => group.GroupId).ToArray(), kept.Snapshot.Groups.Select(group => group.GroupId).ToArray());
        CollectionAssert.AreEqual(new[] { kept.Decision.DecisionId }, kept.Snapshot.DecisionReferences.Select(item => item.DecisionId).ToArray());

        var unresolved = fixture.Apply(fixture.Baseline, [], "x", "y", DeduplicationAuthorityPolicyConstants.MarkUnresolvedAction);
        CollectionAssert.AreEqual(fixture.Baseline.UnresolvedCandidates.Select(item => item.CandidateId).ToArray(), unresolved.Snapshot.UnresolvedCandidates.Select(item => item.CandidateId).ToArray());

        var reopened = CorpusSnapshotService.RehydrateSuccessor(
            ToUnverified(kept.Snapshot),
            fixture.Source,
            fixture.Policy,
            fixture.Baseline,
            kept.ActiveDecisions,
            kept.KnownDecisions,
            new[] { fixture.Baseline },
            kept.Decision);
        Assert.AreEqual(kept.Snapshot.RecordDigest, reopened.RecordDigest);
    }

    [TestMethod]
    public void Merge_rejects_transitive_keep_separate_constraint_and_already_resolved_rows()
    {
        var fixture = Fixture.Create();
        var bc = fixture.Apply(fixture.Baseline, [], "b", "c", DeduplicationAuthorityPolicyConstants.MergeAction);
        var keepAc = fixture.Apply(bc.Snapshot, bc.ActiveDecisions, "a", "c", DeduplicationAuthorityPolicyConstants.KeepSeparateAction, bc.KnownDecisions);

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            fixture.Apply(keepAc.Snapshot, keepAc.ActiveDecisions, "a", "b", DeduplicationAuthorityPolicyConstants.MergeAction, keepAc.KnownDecisions));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, error.Category);

        var alreadyMerged = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            fixture.Apply(bc.Snapshot, bc.ActiveDecisions, "b", "c", DeduplicationAuthorityPolicyConstants.MergeAction, bc.KnownDecisions));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, alreadyMerged.Category);

        var cannotSplit = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            fixture.Apply(bc.Snapshot, bc.ActiveDecisions, "b", "c", DeduplicationAuthorityPolicyConstants.KeepSeparateAction, bc.KnownDecisions));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, cannotSplit.Category);
    }

    [TestMethod]
    public void Same_target_correction_requires_exact_active_supersession_and_merge_replaces_constraint()
    {
        var fixture = Fixture.Create();
        var kept = fixture.Apply(fixture.Baseline, [], "a", "b", DeduplicationAuthorityPolicyConstants.KeepSeparateAction);

        var implicitCorrection = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            fixture.Apply(kept.Snapshot, kept.ActiveDecisions, "a", "b", DeduplicationAuthorityPolicyConstants.MergeAction, kept.KnownDecisions));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, implicitCorrection.Category);

        var corrected = fixture.Apply(
            kept.Snapshot,
            kept.ActiveDecisions,
            "a",
            "b",
            DeduplicationAuthorityPolicyConstants.MergeAction,
            kept.KnownDecisions,
            kept.Decision);
        CollectionAssert.AreEqual(new[] { "a", "b" }, corrected.Snapshot.Groups.Single(group => group.MemberCandidateIds.Contains("a")).MemberCandidateIds.ToArray());
        CollectionAssert.AreEqual(new[] { corrected.Decision.DecisionId }, corrected.Snapshot.DecisionReferences.Select(item => item.DecisionId).ToArray());
    }

    [TestMethod]
    public void Strict_successor_rejects_stale_decision_and_tampered_reduced_membership_on_rehydrate()
    {
        var fixture = Fixture.Create();
        var merged = fixture.Apply(fixture.Baseline, [], "a", "b", DeduplicationAuthorityPolicyConstants.MergeAction);
        var persisted = ToUnverified(merged.Snapshot);
        var tampered = persisted with
        {
            Groups = fixture.Baseline.Groups,
            ContentDigest = merged.Snapshot.ContentDigest,
            RecordDigest = merged.Snapshot.RecordDigest
        };

        var tamperError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() => CorpusSnapshotService.RehydrateSuccessor(
            tampered,
            fixture.Source,
            fixture.Policy,
            fixture.Baseline,
            merged.ActiveDecisions,
            merged.KnownDecisions,
            new[] { fixture.Baseline },
            merged.Decision));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, tamperError.Category);

        var staleError = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() => DeduplicationSnapshotReducer.Reduce(
            fixture.Source,
            merged.Snapshot,
            merged.Decision,
            merged.ActiveDecisions,
            merged.KnownDecisions));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, staleError.Category);
    }

    private sealed class Fixture
    {
        private readonly List<VerifiedCorpusSnapshot> _knownSnapshots;

        private Fixture(
            VerifiedDeduplicationAuthorityPolicy policy,
            VerifiedDeduplicationAuthorityResultDigest source,
            VerifiedCorpusSnapshot baseline)
        {
            Policy = policy;
            Source = source;
            Baseline = baseline;
            _knownSnapshots = new List<VerifiedCorpusSnapshot> { baseline };
        }

        public VerifiedDeduplicationAuthorityPolicy Policy { get; }
        public VerifiedDeduplicationAuthorityResultDigest Source { get; }
        public VerifiedCorpusSnapshot Baseline { get; }

        public static Fixture Create()
        {
            var policy = BuildPolicy();
            var source = BuildSource(policy.PolicyId);
            var baseline = CorpusSnapshotService.CreateBaseline("snapshot-0", source, policy, "alice", "owner", ClockAt(0));
            return new Fixture(policy, source, baseline);
        }

        public Transition Apply(
            VerifiedCorpusSnapshot predecessor,
            IReadOnlyList<VerifiedDeduplicationAuthorityDecision> predecessorActive,
            string first,
            string second,
            string action,
            IReadOnlyList<VerifiedDeduplicationAuthorityDecision>? known = null,
            VerifiedDeduplicationAuthorityDecision? superseded = null)
        {
            var target = Target(first, second);
            var ordinal = (known?.Count ?? 0) + 1;
            var invalidationEffects = new List<DeduplicationAuthorityDecisionInvalidationEffect>
            {
                new(DeduplicationDecisionConstants.InvalidationSnapshotKind, predecessor.SnapshotId, predecessor.RecordDigest)
            };
            if (superseded is not null)
            {
                invalidationEffects.Add(new(
                    DeduplicationDecisionConstants.InvalidationDecisionKind,
                    superseded.DecisionId,
                    superseded.DecisionDigest));
            }

            var decision = DeduplicationDecision.CreateDecisionMaterial(
                new UnverifiedDeduplicationAuthorityDecision(
                    DeduplicationDecisionConstants.SchemaId,
                    DeduplicationDecisionConstants.SchemaVersion,
                    $"decision-{ordinal}-{first}-{second}-{action}",
                    action,
                    Policy.PolicyId,
                    Policy.PolicyVersion,
                    target.TargetKind,
                    target.TargetId,
                    target.TargetDigest,
                    Source.Result.ResultId,
                    Source.ResultDigest,
                    predecessor.SnapshotId,
                    predecessor.RecordDigest,
                    target.Evidence.Select(item => new DeduplicationAuthorityDecisionEvidenceReference(
                        item.Kind.ToString(),
                        item.EvidenceId,
                        DigestScope.CanonicalJsonRecord.Value,
                        DeduplicationAuthorityDigests.CreateEvidenceDigestMaterial(item).EvidenceDigest)).ToArray(),
                    "alice",
                    "owner",
                    Policy.PolicyId,
                    DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind,
                    Policy.PolicyDigest,
                    null,
                    action switch
                    {
                        DeduplicationAuthorityPolicyConstants.MergeAction => "duplicate",
                        DeduplicationAuthorityPolicyConstants.KeepSeparateAction => "different",
                        _ => "uncertain"
                    },
                    ClockAt(ordinal).UtcNow,
                    superseded?.DecisionId,
                    invalidationEffects),
                ClockAt(ordinal),
                Policy,
                Source,
                target);

            var active = predecessorActive.Where(item => superseded is null || item.DecisionId != superseded.DecisionId).Append(decision).ToArray();
            var knownDecisions = (known ?? predecessorActive).Append(decision).ToArray();
            var invalidationReferences = invalidationEffects.Select(effect => new CorpusSnapshotInvalidationReference(
                effect.RecordKind,
                effect.RecordId,
                effect.RecordDigest)).ToArray();
            var snapshot = CorpusSnapshotService.CreateSuccessor(
                $"snapshot-{ordinal}-{first}-{second}-{action}",
                predecessor,
                Policy,
                "alice",
                "owner",
                ClockAt(ordinal + 10),
                active,
                invalidationReferences,
                knownDecisions,
                _knownSnapshots.ToArray(),
                Source,
                decision);
            _knownSnapshots.Add(snapshot);
            return new Transition(snapshot, decision, active, knownDecisions);
        }

        private VerifiedDeduplicationAuthorityReviewTargetDigest Target(string first, string second)
        {
            var pair = Source.Result.ReviewRequiredCandidates.Single(item =>
                new[] { item.CandidateAId, item.CandidateBId }.ToHashSet(StringComparer.Ordinal)
                    .SetEquals(new[] { first, second }));
            var ids = new[] { first, second }.OrderBy(item => item, StringComparer.Ordinal).ToArray();
            var evidence = Source.Result.Evidence.Where(item =>
                item.ObjectCandidateId is not null &&
                ids.Contains(item.SubjectCandidateId) &&
                ids.Contains(item.ObjectCandidateId) &&
                item.SubjectCandidateId != item.ObjectCandidateId).ToArray();
            return DeduplicationAuthorityDigests.CreateReviewTargetDigestMaterial(Source, pair, ids, evidence);
        }
    }

    private sealed record Transition(
        VerifiedCorpusSnapshot Snapshot,
        VerifiedDeduplicationAuthorityDecision Decision,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> ActiveDecisions,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> KnownDecisions);

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
            ClockAt(0).UtcNow));

    private static VerifiedDeduplicationAuthorityResultDigest BuildSource(string policyId)
    {
        var candidates = new[] { Candidate("a"), Candidate("b"), Candidate("c"), Candidate("x", false), Candidate("y", false) };
        var pairs = new[] { ("a", "b"), ("b", "c"), ("a", "c"), ("a", "x"), ("x", "y") };
        var evidence = pairs.Select(pair => new DedupEvidence(
            $"evidence-{pair.Item1}-{pair.Item2}",
            DedupEvidenceKind.SourceSighting,
            pair.Item1,
            pair.Item2,
            "review-pair",
            true,
            0.90,
            policyId,
            DeduplicationService.PolicyVersion)).ToArray();
        return DeduplicationAuthorityDigests.CreateResultDigestMaterial(new DeduplicationResult(
            "result-fe-02-2",
            DeduplicationAuthorityDigests.ResultSchemaId,
            DeduplicationAuthorityDigests.ResultSchemaVersion,
            policyId,
            DeduplicationService.PolicyVersion,
            0.95,
            new Dictionary<string, int>(),
            [],
            [],
            candidates,
            [],
            evidence,
            candidates.Where(item => !item.HasStableIdentifier).ToArray(),
            pairs.Select(pair => new DedupReviewCandidate(pair.Item1, pair.Item2, 0.90, 0.95)).ToArray(),
            [],
            [],
            []));
    }

    private static DedupCandidateRecord Candidate(string id, bool stable = true) => new(
        id,
        $"Title {id}",
        stable,
        stable ? $"doi-{id}" : null,
        new[] { $"work-{id}" },
        new[] { $"raw-{id}" },
        new DedupSightingRef("search", $"trace-{id}", SourceSightingId: $"sighting-{id}", ProviderAlias: "fixture", SourceDatabaseOrTool: "fixture"));

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

    private static string BuildGroupId(params string[] members)
    {
        var material = new CanonicalJsonObject().Add(
            "member_candidate_ids",
            CanonicalJsonValue.Array(members.OrderBy(item => item, StringComparer.Ordinal).Select(CanonicalJsonValue.From).ToArray()));
        return $"group-{ContentDigest.Sha256CanonicalJson(material).Value}";
    }

    private static IClock ClockAt(int hour) =>
        new FixedClock(new DateTimeOffset(2026, 7, 15, hour, 0, 0, TimeSpan.Zero));

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
