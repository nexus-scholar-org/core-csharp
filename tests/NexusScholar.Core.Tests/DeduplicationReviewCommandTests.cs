using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;

namespace NexusScholar.Core.Tests;

[TestClass]
public sealed class DeduplicationReviewCommandTests
{
    [TestMethod]
    public void Create_and_rehydrate_reproduce_request_and_derived_decision_identity()
    {
        var fixture = Fixture.Create();
        var command = fixture.Create(fixture.Command());
        var reopened = fixture.Rehydrate(command.Material);

        Assert.AreEqual(command.RequestDigest, reopened.RequestDigest);
        Assert.AreEqual(command.DecisionId, reopened.DecisionId);
        Assert.IsTrue(command.DecisionId.StartsWith("decision-", StringComparison.Ordinal));
        var decision = DeduplicationReviewCommand.BuildDecisionMaterial(command, fixture.Target);
        Assert.AreEqual(fixture.Target.Evidence.Count, decision.EvidenceReferences.Count);
        Assert.AreEqual(fixture.SnapshotId, decision.SourceSnapshotId);
    }

    [TestMethod]
    public void Rehydrate_rejects_tamper_stale_binding_and_unauthorized_actor()
    {
        var fixture = Fixture.Create();
        var command = fixture.Create(fixture.Command());

        var tampered = Assert.ThrowsExactly<DeduplicationAuthorityException>(() =>
            fixture.Rehydrate(command.Material with { ReasonCode = "different" }));
        Assert.AreEqual(DeduplicationReviewCommandErrorCodes.InvalidCommand, tampered.Category);

        var stale = Assert.ThrowsExactly<DeduplicationAuthorityException>(() =>
            fixture.Create(fixture.Command() with { SourceSnapshotId = "other" }));
        Assert.AreEqual(DeduplicationReviewCommandErrorCodes.StaleCommandBinding, stale.Category);

        var unauthorized = Assert.ThrowsExactly<DeduplicationAuthorityException>(() =>
            fixture.Create(fixture.Command() with { ActorId = "automation" }));
        Assert.AreEqual(DeduplicationReviewCommandErrorCodes.UnauthorizedActor, unauthorized.Category);
    }

    [TestMethod]
    public void Create_requires_digest_bound_supersession()
    {
        var fixture = Fixture.Create();
        var error = Assert.ThrowsExactly<DeduplicationAuthorityException>(() =>
            fixture.Create(fixture.Command() with { SupersedesDecisionId = "decision-old" }));
        Assert.AreEqual(DeduplicationReviewCommandErrorCodes.InvalidCommand, error.Category);
    }

    private sealed record Fixture(
        VerifiedDeduplicationAuthorityPolicy Policy,
        VerifiedDeduplicationAuthorityResultDigest Source,
        VerifiedDeduplicationAuthorityReviewTargetDigest Target,
        ContentDigest DecisionSetDigest,
        string SnapshotId,
        ContentDigest SnapshotDigest)
    {
        public static Fixture Create()
        {
            var clock = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);
            var policy = DeduplicationAuthorityPolicy.CreatePolicyMaterial(new UnverifiedDeduplicationAuthorityPolicy(
                DeduplicationAuthorityPolicyConstants.SchemaId, DeduplicationAuthorityPolicyConstants.SchemaVersion,
                DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind, DeduplicationService.PolicyId, DeduplicationService.PolicyVersion,
                new[] { new DeduplicationAuthorityPolicyActorRole("alice", "owner") },
                DeduplicationAuthorityPolicyConstants.ClosedActions,
                new[]
                {
                    new DeduplicationAuthorityPolicyReasonGroup("merge", new[] { "duplicate" }),
                    new DeduplicationAuthorityPolicyReasonGroup("keep-separate", new[] { "different" }),
                    new DeduplicationAuthorityPolicyReasonGroup("mark-unresolved", new[] { "uncertain" })
                }, true, "alice", "owner", clock));
            var a = Candidate("a", "One", "doi:one");
            var b = Candidate("b", "One copy", "doi:two");
            var evidence = new DedupEvidence("evidence", DedupEvidenceKind.FuzzyTitle, "a", "b", "similar", true, .96, DeduplicationService.PolicyId, DeduplicationService.PolicyVersion);
            var pair = new DedupReviewCandidate("a", "b", .96, .95);
            var result = new DeduplicationResult("result", DeduplicationService.ResultSchemaId, DeduplicationService.ResultSchemaVersion,
                DeduplicationService.PolicyId, DeduplicationService.PolicyVersion, .95, DeduplicationService.DefaultProviderPriority,
                Array.Empty<string>(), Array.Empty<string>(), new[] { a, b }, Array.Empty<DedupCluster>(), new[] { evidence },
                Array.Empty<DedupCandidateRecord>(), new[] { pair }, Array.Empty<DedupMessage>(), Array.Empty<DedupMessage>(), Array.Empty<string>());
            var source = DeduplicationAuthorityDigests.CreateResultDigestMaterial(result);
            var target = DeduplicationAuthorityDigests.CreateReviewTargetDigestMaterial(source, pair, new[] { "a", "b" }, new[] { evidence });
            return new Fixture(policy, source, target, ContentDigest.Sha256Utf8("decision-set"), "snapshot", ContentDigest.Sha256Utf8("snapshot"));
        }

        public UnverifiedDeduplicationReviewCommand Command() => new(
            DeduplicationReviewCommandConstants.SchemaId, DeduplicationReviewCommandConstants.SchemaVersion,
            "authority-1", ContentDigest.Sha256Utf8("manifest"), DecisionSetDigest,
            Source.Result.ResultId, Source.ResultDigest, SnapshotId, SnapshotDigest,
            Target.TargetKind, Target.TargetId, Target.TargetDigest, Policy.PolicyId, Policy.PolicyVersion, Policy.PolicyDigest,
            "merge", "duplicate", "Human reviewed duplicate evidence", "alice", "owner", null, null);

        public VerifiedDeduplicationReviewCommand Create(UnverifiedDeduplicationReviewCommand command) =>
            DeduplicationReviewCommand.Create(command, Policy, Source, Target, DecisionSetDigest,
                "authority-1", ContentDigest.Sha256Utf8("manifest"), SnapshotId, SnapshotDigest);

        public VerifiedDeduplicationReviewCommand Rehydrate(UnverifiedDeduplicationReviewCommand command) =>
            DeduplicationReviewCommand.Rehydrate(command, Policy, Source, Target, DecisionSetDigest,
                "authority-1", ContentDigest.Sha256Utf8("manifest"), SnapshotId, SnapshotDigest);

        private static DedupCandidateRecord Candidate(string id, string title, string workId) => new(
            id, title, true, workId, new[] { workId }, Array.Empty<string>(),
            new DedupSightingRef("import", "trace", $"sighting-{id}"));
    }
}
