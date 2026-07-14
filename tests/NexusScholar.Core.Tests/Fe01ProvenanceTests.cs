using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Kernel;
using NexusScholar.Provenance;

namespace NexusScholar.Core.Tests;

[TestClass]
public sealed class Fe01ProvenanceTests
{
    private static readonly IClock Clock = new FixedClock();

    [TestMethod]
    public void Baseline_corpus_snapshot_published_can_be_constructed_with_expected_projection_and_digests()
    {
        var ids = new FixedIdGenerator(
            Guid.Parse("00000000-0000-0000-0000-000000000101"),
            Guid.Parse("00000000-0000-0000-0000-000000000102"));
        var activity = new ProvenanceActivity(
            "corpus-snapshot-published",
            "Corpus snapshot published",
            RequiresActor: true,
            RequiresInput: true,
            RequiresOutput: true);

        var publishedSnapshot = new ProvenanceEntityRef(
            "nexus.corpus.snapshot",
            "snapshot-fe01-baseline",
            ContentDigest.Sha256Utf8("corpus-snapshot-published-output"));

        var sourceResultRef = new ProvenanceEntityRef(
            "nexus.deduplication.result",
            "result-fe01",
            ContentDigest.Sha256Utf8("result-content"));
        var policyRef = new ProvenanceEntityRef(
            "local-deduplication-authority-policy",
            "policy-fe01",
            ContentDigest.Sha256Utf8("policy-content"));
        var manifestRef = new ProvenanceEntityRef(
            "source-analysis-manifest",
            "manifest-fe01",
            ContentDigest.Sha256Utf8("manifest-content"));
        var decisionSetRef = new ProvenanceEntityRef(
            "deduplication-decision-set",
            "decision-set-empty",
            ContentDigest.Sha256Utf8("decision-set-empty"));

        var record = ResearchEventFactory.Create(
            ids,
            Clock,
            activity,
            publishedSnapshot,
            new ProvenanceAgent("human-1", ProvenanceAgent.HumanKind),
            inputs: new[] { sourceResultRef, policyRef, manifestRef, decisionSetRef },
            outputs: new[] { publishedSnapshot });

        Assert.AreEqual("corpus-snapshot-published", record.Activity.ActivityId);
        Assert.AreEqual("nexus.corpus.snapshot", record.Subject.EntityKind);
        Assert.AreEqual("snapshot-fe01-baseline", record.Subject.EntityId);
        Assert.AreEqual(4, record.Inputs.Count);
        Assert.AreEqual(1, record.Outputs.Count);
        Assert.AreEqual(publishedSnapshot.EntityId, record.Outputs[0].EntityId);
        Assert.IsNull(record.ProtocolBinding);
        Assert.AreEqual(record.ToDigestEnvelope().ComputeDigest(), record.EventDigest);
    }

    [TestMethod]
    public void Future_deduplication_decision_recorded_fixture_direction_is_inbound_inputs_to_decision_output()
    {
        var ids = new FixedIdGenerator(
            Guid.Parse("00000000-0000-0000-0000-000000000201"),
            Guid.Parse("00000000-0000-0000-0000-000000000202"));
        var activity = new ProvenanceActivity(
            "deduplication-decision-recorded",
            "Deduplication decision recorded",
            RequiresActor: true,
            RequiresInput: true,
            RequiresOutput: true);

        var decision = new ProvenanceEntityRef(
            "nexus.deduplication.decision",
            "decision-fe01",
            ContentDigest.Sha256Utf8("decision-content"));
        var policyRef = new ProvenanceEntityRef(
            "local-deduplication-authority-policy",
            "policy-fe01",
            ContentDigest.Sha256Utf8("policy-content"));
        var resultRef = new ProvenanceEntityRef(
            "nexus.deduplication.result",
            "result-fe01",
            ContentDigest.Sha256Utf8("result-content"));
        var targetRef = new ProvenanceEntityRef(
            "review-candidate-pair",
            "target-fe01",
            ContentDigest.Sha256Utf8("target-content"));
        var snapshotRef = new ProvenanceEntityRef(
            "nexus.corpus.snapshot",
            "snapshot-fe01-baseline",
            ContentDigest.Sha256Utf8("snapshot-baseline"));
        var evidenceRef = new ProvenanceEntityRef(
            "nexus.deduplication.evidence",
            "evidence-fe01",
            ContentDigest.Sha256Utf8("evidence-content"));

        var record = ResearchEventFactory.Create(
            ids,
            Clock,
            activity,
            decision,
            new ProvenanceAgent("human-2", ProvenanceAgent.HumanKind),
            inputs: new[] { policyRef, resultRef, targetRef, snapshotRef, evidenceRef },
            outputs: new[] { decision });

        Assert.AreEqual("deduplication-decision-recorded", record.Activity.ActivityId);
        Assert.AreEqual("nexus.deduplication.decision", record.Subject.EntityKind);
        Assert.AreEqual("decision-fe01", record.Subject.EntityId);
        CollectionAssert.AreEqual(
            new[]
            {
                "local-deduplication-authority-policy",
                "nexus.deduplication.result",
                "review-candidate-pair",
                "nexus.corpus.snapshot",
                "nexus.deduplication.evidence"
            },
            Array.ConvertAll(record.Inputs.ToArray(), item => item.EntityKind));
        Assert.AreEqual(1, record.Outputs.Count);
        Assert.IsNull(record.ProtocolBinding);
        Assert.AreEqual(record.ToDigestEnvelope().ComputeDigest(), record.EventDigest);
    }

    [TestMethod]
    public void Future_corpus_snapshot_invalidated_fixture_direction_is_inbound_inputs_to_invalidation_output()
    {
        var ids = new FixedIdGenerator(
            Guid.Parse("00000000-0000-0000-0000-000000000301"),
            Guid.Parse("00000000-0000-0000-0000-000000000302"));
        var activity = new ProvenanceActivity(
            "corpus-snapshot-invalidated",
            "Corpus snapshot invalidated",
            RequiresActor: true,
            RequiresInput: true,
            RequiresOutput: true);

        var invalidation = new ProvenanceEntityRef(
            "nexus.corpus.snapshot-invalidation",
            "invalidation-fe01",
            ContentDigest.Sha256Utf8("invalidation-content"));
        var causeDecisionRef = new ProvenanceEntityRef(
            "nexus.deduplication.decision",
            "decision-fe01",
            ContentDigest.Sha256Utf8("decision-content"));
        var successorSnapshotRef = new ProvenanceEntityRef(
            "nexus.corpus.snapshot",
            "snapshot-fe01-successor",
            ContentDigest.Sha256Utf8("snapshot-successor"));

        var record = ResearchEventFactory.Create(
            ids,
            Clock,
            activity,
            invalidation,
            new ProvenanceAgent("human-3", ProvenanceAgent.HumanKind),
            inputs: new[] { causeDecisionRef, successorSnapshotRef },
            outputs: new[] { invalidation });

        Assert.AreEqual("corpus-snapshot-invalidated", record.Activity.ActivityId);
        Assert.AreEqual("nexus.corpus.snapshot-invalidation", record.Subject.EntityKind);
        Assert.AreEqual("invalidation-fe01", record.Subject.EntityId);
        Assert.AreEqual(2, record.Inputs.Count);
        Assert.AreEqual(1, record.Outputs.Count);
        Assert.IsNull(record.ProtocolBinding);
        Assert.AreEqual(record.ToDigestEnvelope().ComputeDigest(), record.EventDigest);
    }

    [TestMethod]
    public void Reject_reversed_input_output_direction_and_noncanonical_projection_kinds()
    {
        var ids = new FixedIdGenerator(Guid.Parse("00000000-0000-0000-0000-000000000401"));
        var artifact = new ProvenanceEntityRef(
            "artifact",
            "artifact-1",
            ContentDigest.Sha256Utf8("artifact"));

        var reversedError = Assert.ThrowsExactly<ProvenanceRuleException>(() =>
            ResearchEventFactory.Create(
                ids,
                Clock,
                new ProvenanceActivity("snapshot-projection-started", "Snapshot projection started", true, true, false),
                artifact,
                new ProvenanceAgent("human-4", ProvenanceAgent.HumanKind),
                inputs: Array.Empty<ProvenanceEntityRef>(),
                outputs: new[] { artifact }));

        Assert.AreEqual(ProvenanceErrorCodes.MissingRequiredInput, reversedError.Category);

        var nonCanonicalError = Assert.ThrowsExactly<ProvenanceRuleException>(() =>
            ResearchEventFactory.Create(
                ids,
                Clock,
                new ProvenanceActivity("snapshot-projection-finished", "Snapshot projection finished", true, false, true),
                new ProvenanceEntityRef("projection", "projection-1"),
                new ProvenanceAgent("human-5", ProvenanceAgent.HumanKind),
                inputs: Array.Empty<ProvenanceEntityRef>(),
                outputs: new[] { artifact }));

        Assert.AreEqual(ProvenanceErrorCodes.ProjectionNotCanonical, nonCanonicalError.Category);
    }

    private sealed class FixedIdGenerator : IIdGenerator
    {
        private readonly Queue<Guid> _ids;

        public FixedIdGenerator(params Guid[] ids)
        {
            _ids = new Queue<Guid>(ids);
        }

        public Guid NewId()
        {
            return _ids.Count == 0 ? Guid.NewGuid() : _ids.Dequeue();
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);
    }
}
