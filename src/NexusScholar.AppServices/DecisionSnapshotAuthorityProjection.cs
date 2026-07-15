using System;
using System.Collections.Generic;
using System.Linq;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;

namespace NexusScholar.AppServices;

public sealed class DecisionSnapshotAuthorityProjectionService
{
    public DecisionSnapshotAuthorityReadModel Project(
        DecisionSnapshotAuthorityHealthDescriptor authorityGenerationHealth,
        VerifiedDeduplicationAuthorityPolicy policy,
        VerifiedCorpusSnapshot baselineSnapshot)
    {
        ArgumentNullException.ThrowIfNull(authorityGenerationHealth);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(baselineSnapshot);

        var issues = new List<string>();
        if (!authorityGenerationHealth.IsHealthy)
        {
            issues.Add(string.IsNullOrWhiteSpace(authorityGenerationHealth.HealthCode)
                ? "authority-generation-unhealthy"
                : authorityGenerationHealth.HealthCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(authorityGenerationHealth.HealthMessage))
        {
            issues.Add(authorityGenerationHealth.HealthMessage.Trim());
        }

        var policyBindingConsistent = string.Equals(policy.PolicyId, baselineSnapshot.AuthoritySourceId, StringComparison.Ordinal) &&
            string.Equals(policy.PolicyDigest.ToString(), baselineSnapshot.AuthoritySourceDigest.ToString(), StringComparison.Ordinal);
        if (!policyBindingConsistent)
        {
            issues.Add("snapshot-policy-binding-mismatch");
        }

        var isHealthy = authorityGenerationHealth.IsHealthy && policyBindingConsistent;

        return new DecisionSnapshotAuthorityReadModel(
            new DecisionSnapshotAuthorityGenerationHealthReadModel(
                authorityGenerationHealth.AuthorityGenerationId,
                isHealthy,
                policyBindingConsistent,
                issues.AsReadOnly()),
            new DecisionSnapshotAuthorityPolicyReadModel(
                policy.PolicyId,
                policy.PolicyVersion,
                policy.IssuedByActorId,
                policy.IssuedByRole,
                policy.PolicyDigest.ToString()),
            new DecisionSnapshotAuthorityBaselineSnapshotReadModel(
                baselineSnapshot.SnapshotId,
                baselineSnapshot.ContentDigest.ToString(),
                baselineSnapshot.UnresolvedCandidates.Count,
                baselineSnapshot.DecisionReferences.Count == 0));
    }
}

public sealed record DecisionSnapshotAuthorityHealthDescriptor(
    string AuthorityGenerationId,
    bool IsHealthy,
    string? HealthCode = null,
    string? HealthMessage = null);

public sealed record DecisionSnapshotAuthorityReadModel(
    DecisionSnapshotAuthorityGenerationHealthReadModel Health,
    DecisionSnapshotAuthorityPolicyReadModel Policy,
    DecisionSnapshotAuthorityBaselineSnapshotReadModel BaselineSnapshot);

public sealed record DecisionSnapshotAuthorityGenerationHealthReadModel(
    string AuthorityGenerationId,
    bool IsHealthy,
    bool IsPolicySourceBindingConsistent,
    IReadOnlyList<string> HealthIssues);

public sealed record DecisionSnapshotAuthorityPolicyReadModel(
    string PolicyId,
    string PolicyVersion,
    string IssuedByActorId,
    string IssuedByRole,
    string PolicyDigest);

public sealed record DecisionSnapshotAuthorityBaselineSnapshotReadModel(
    string SnapshotId,
    string ContentDigest,
    int UnresolvedCandidateCount,
    bool IsDecisionSetEmpty);
