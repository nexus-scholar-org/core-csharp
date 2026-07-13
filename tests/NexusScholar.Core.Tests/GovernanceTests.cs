using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.AI;
using NexusScholar.Bundles;
using NexusScholar.Kernel;
using NexusScholar.Provenance;

namespace NexusScholar.Core.Tests;

[TestClass]
public sealed class GovernanceTests
{
    [TestMethod]
    public void Scientific_model_proposal_requires_human_approval_policy()
    {
        Assert.ThrowsExactly<DomainRuleException>(() => AiTaskPolicy.Create(
            "screen-title-abstract",
            AiAuthority.ScientificDecisionProposal,
            humanApprovalRequired: false,
            evidenceRequired: true,
            externalDataTransferAllowed: false));
    }

    [TestMethod]
    public void Ai_proposals_are_immutable_evidence_and_expose_no_authority_transition()
    {
        var evidence = new List<ContentDigest> { ContentDigest.Sha256Utf8("source-evidence") };
        var policy = AiTaskPolicy.Create(
            "screen-title-abstract",
            AiAuthority.ScientificDecisionProposal,
            humanApprovalRequired: true,
            evidenceRequired: true,
            externalDataTransferAllowed: false);
        var proposal = new AiProposal<string>(
            policy,
            "include suggestion",
            evidence,
            new FixedClock().UtcNow);

        evidence.Clear();

        Assert.AreEqual(1, proposal.Evidence.Count);
        Assert.IsNull(typeof(AiProposal<string>).GetMethod("Accept", BindingFlags.Public | BindingFlags.Instance));
        Assert.IsNull(typeof(AiProposal<>).Assembly.GetType("NexusScholar.AI.AcceptedAiProposal`1"));
    }

    [TestMethod]
    public void Ai_proposals_reject_invalid_evidence_digests()
    {
        var policy = AiTaskPolicy.Create(
            "screen-title-abstract",
            AiAuthority.ScientificDecisionProposal,
            humanApprovalRequired: true,
            evidenceRequired: true,
            externalDataTransferAllowed: false);
        Assert.ThrowsExactly<DomainRuleException>(() => new AiProposal<string>(
            policy,
            "include suggestion",
            [default],
            new FixedClock().UtcNow));
    }

    [TestMethod]
    public void Ai_proposals_bind_policy_evidence_and_utc_timestamp_requirements()
    {
        var policy = AiTaskPolicy.Create(
            "screen-title-abstract",
            AiAuthority.ScientificDecisionProposal,
            humanApprovalRequired: true,
            evidenceRequired: true,
            externalDataTransferAllowed: false);

        Assert.ThrowsExactly<DomainRuleException>(() => new AiProposal<string>(
            policy,
            "include suggestion",
            [],
            new FixedClock().UtcNow));
        Assert.ThrowsExactly<DomainRuleException>(() => new AiProposal<string>(
            policy,
            "include suggestion",
            [ContentDigest.Sha256Utf8("source-evidence")],
            default));
        Assert.AreEqual(0, typeof(AiTaskPolicy).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length);
    }

    [TestMethod]
    public void Provenance_store_is_append_only_and_rejects_duplicate_identity()
    {
        var ids = new GuidV7IdGenerator();
        var clock = new FixedClock();
        var researchEvent = ResearchEventFactory.Create(
            ids,
            clock,
            "protocol-approved",
            "protocol",
            "p-1",
            ActorId.From("researcher-1"));
        var store = new InMemoryProvenanceStore();
        store.Append(researchEvent);

        var error = Assert.ThrowsExactly<ProvenanceRuleException>(() => store.Append(researchEvent));
        Assert.AreEqual(ProvenanceErrorCodes.DuplicateEventId, error.Category);
    }

    [TestMethod]
    public void Bundle_verifier_rejects_duplicate_paths()
    {
        var digest = ContentDigest.Sha256Utf8("same content");
        var manifest = new ReviewBundleManifest(
            "nexus.review-bundle/v1",
            "project-1",
            digest,
            "workflow-1",
            new FixedClock().UtcNow,
            new[]
            {
                new BundleArtifact("protocol/protocol.json", "application/json", 12, digest),
                new BundleArtifact("protocol/protocol.json", "application/json", 12, digest)
            });

        Assert.IsFalse(new BundleVerifier().Verify(manifest).IsValid);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 6, 25, 12, 0, 0, TimeSpan.Zero);
    }
}
