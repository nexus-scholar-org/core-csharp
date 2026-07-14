using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.AppServices;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;

namespace NexusScholar.AppServices.Tests;

[TestClass]
public sealed class DecisionSnapshotAuthorityProjectionTests
{
    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero));

    [TestMethod]
    public void Project_MapsHealthyDecisionAuthorityInputs()
    {
        var policy = BuildPolicy("policy-2026-fe-01", "actor-1", "lead");
        var snapshot = BuildBaselineSnapshot("snapshot-2026-fe-01-baseline", policy);

        var projection = new DecisionSnapshotAuthorityProjectionService().Project(
            new DecisionSnapshotAuthorityHealthDescriptor("auth-gen-1", true, HealthCode: "healthy"),
            policy,
            snapshot);

        Assert.AreEqual("auth-gen-1", projection.Health.AuthorityGenerationId);
        Assert.IsTrue(projection.Health.IsHealthy);
        Assert.IsTrue(projection.Health.IsPolicySourceBindingConsistent);
        CollectionAssert.AreEqual(Array.Empty<string>(), projection.Health.HealthIssues.ToArray());

        Assert.AreEqual(policy.PolicyId, projection.Policy.PolicyId);
        Assert.AreEqual(policy.PolicyVersion, projection.Policy.PolicyVersion);
        Assert.AreEqual(policy.IssuedByActorId, projection.Policy.IssuedByActorId);
        Assert.AreEqual(policy.IssuedByRole, projection.Policy.IssuedByRole);
        Assert.AreEqual(policy.PolicyDigest.ToString(), projection.Policy.PolicyDigest);

        Assert.AreEqual(snapshot.SnapshotId, projection.BaselineSnapshot.SnapshotId);
        Assert.AreEqual(snapshot.ContentDigest.ToString(), projection.BaselineSnapshot.ContentDigest);
        Assert.AreEqual(snapshot.UnresolvedCandidates.Count, projection.BaselineSnapshot.UnresolvedCandidateCount);
        Assert.IsTrue(projection.BaselineSnapshot.IsDecisionSetEmpty);
    }

    [TestMethod]
    public void Project_PropagatesUnhealthyGenerationHealth()
    {
        var policy = BuildPolicy("policy-2026-fe-01", "actor-1", "lead");
        var snapshot = BuildBaselineSnapshot("snapshot-2026-fe-01-baseline-unhealthy", policy);

        var projection = new DecisionSnapshotAuthorityProjectionService().Project(
            new DecisionSnapshotAuthorityHealthDescriptor("auth-gen-2", false, HealthCode: "authority-generation-stale"),
            policy,
            snapshot);

        Assert.IsFalse(projection.Health.IsHealthy);
        CollectionAssert.Contains(projection.Health.HealthIssues.ToArray(), "authority-generation-stale");
    }

    [TestMethod]
    public void Project_ReportsPolicySourceBindingMismatch()
    {
        var policy = BuildPolicy("policy-2026-fe-01", "actor-1", "lead");
        var snapshot = BuildBaselineSnapshot("snapshot-2026-fe-01-baseline-mismatch", policy);
        var mismatchedPolicy = BuildPolicy("policy-mismatch-2026", "actor-1", "lead");

        var projection = new DecisionSnapshotAuthorityProjectionService().Project(
            new DecisionSnapshotAuthorityHealthDescriptor("auth-gen-3", true, HealthCode: "healthy"),
            mismatchedPolicy,
            snapshot);

        Assert.IsFalse(projection.Health.IsHealthy);
        CollectionAssert.Contains(projection.Health.HealthIssues.ToArray(), "snapshot-policy-binding-mismatch");
        Assert.IsFalse(projection.Health.IsPolicySourceBindingConsistent);
    }

    private static VerifiedDeduplicationAuthorityPolicy BuildPolicy(string policyId, string actorId, string role)
    {
        return DeduplicationAuthorityPolicy.CreatePolicyMaterial(new UnverifiedDeduplicationAuthorityPolicy(
            DeduplicationAuthorityPolicyConstants.SchemaId,
            DeduplicationAuthorityPolicyConstants.SchemaVersion,
            DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind,
            policyId,
            "1.0.0",
            new[] { new DeduplicationAuthorityPolicyActorRole(actorId, role) },
            new[]
            {
                DeduplicationAuthorityPolicyConstants.MergeAction,
                DeduplicationAuthorityPolicyConstants.KeepSeparateAction,
                DeduplicationAuthorityPolicyConstants.MarkUnresolvedAction
            },
            new[]
            {
                new DeduplicationAuthorityPolicyReasonGroup(
                    DeduplicationAuthorityPolicyConstants.MergeAction,
                    new[] { "duplicate-records" }),
                new DeduplicationAuthorityPolicyReasonGroup(
                    DeduplicationAuthorityPolicyConstants.KeepSeparateAction,
                    new[] { "different-evidence" }),
                new DeduplicationAuthorityPolicyReasonGroup(
                    DeduplicationAuthorityPolicyConstants.MarkUnresolvedAction,
                    new[] { "insufficient-evidence" })
            },
            RequiresRationale: true,
            IssuedByActorId: actorId,
            IssuedByRole: role,
            IssuedAt: Clock.UtcNow));
    }

    private static VerifiedCorpusSnapshot BuildBaselineSnapshot(string snapshotId, VerifiedDeduplicationAuthorityPolicy policy)
    {
        var result = BuildDeduplicationResult();
        var verifiedResult = DeduplicationAuthorityDigests.CreateResultDigestMaterial(result);

        return CorpusSnapshotService.CreateBaseline(
            snapshotId,
            verifiedResult,
            policy,
            policy.IssuedByActorId,
            policy.IssuedByRole,
            Clock);
    }

    private static DeduplicationResult BuildDeduplicationResult()
    {
        return new DeduplicationResult(
            "dedup-result-2026-fe-01",
            DeduplicationAuthorityDigests.ResultSchemaId,
            DeduplicationAuthorityDigests.ResultSchemaVersion,
            DeduplicationService.PolicyId,
            "1.0.0",
            0.92,
            new Dictionary<string, int>(StringComparer.Ordinal),
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[]
            {
                Candidate("candidate-stable-1", "Resolved duplicate candidate", "record-stable-1", "doi:10.1000/a"),
                Candidate("candidate-unstable-1", "Unresolved no-id candidate", "record-unstable-1", null)
            },
            Array.Empty<DedupCluster>(),
            Array.Empty<DedupEvidence>(),
            Array.Empty<DedupCandidateRecord>(),
            Array.Empty<DedupReviewCandidate>(),
            Array.Empty<DedupMessage>(),
            Array.Empty<DedupMessage>(),
            new[] { "no-live-provider-network" });
    }

    private static DedupCandidateRecord Candidate(string id, string title, string sourceRecordId, string? primaryWorkId)
    {
        var workIds = primaryWorkId is null ? Array.Empty<string>() : new[] { primaryWorkId };

        return new DedupCandidateRecord(
            id,
            title,
            primaryWorkId is not null,
            primaryWorkId,
            workIds,
            new[] { sourceRecordId },
            new DedupSightingRef(
                "app-projection-test",
                SourceTraceId: "trace-baseline",
                SourceRecordId: sourceRecordId,
                SourceSightingId: $"sighting-{sourceRecordId}",
                SourceDatabaseOrTool: "AppServices-Projection-Test",
                SourceFileDigest: ContentDigest.Sha256Utf8($"source-file-{sourceRecordId}").ToString(),
                SourceFileDigestScope: "raw-artifact-bytes",
                RawRecordDigest: ContentDigest.Sha256Utf8($"raw-{sourceRecordId}").ToString()));
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }
}
