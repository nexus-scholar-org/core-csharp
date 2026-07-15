using System.Text;
using NexusScholar.Kernel;

namespace NexusScholar.Deduplication;

public static class DeduplicationReviewCommandConstants
{
    public const string SchemaId = "nexus.deduplication.review-command";
    public const string SchemaVersion = "1.0.0";
}

public static class DeduplicationReviewCommandErrorCodes
{
    public const string InvalidCommand = "invalid-deduplication-review-command";
    public const string NonCanonicalCommand = "non-canonical-deduplication-review-command";
    public const string StaleCommandBinding = "stale-deduplication-review-command-binding";
    public const string UnauthorizedActor = "unauthorized-deduplication-review-command-actor";
}

public sealed record UnverifiedDeduplicationReviewCommand(
    string SchemaId,
    string SchemaVersion,
    string AuthorityGenerationId,
    ContentDigest AuthorityGenerationManifestDigest,
    ContentDigest ActiveDecisionSetDigest,
    string SourceResultId,
    ContentDigest SourceResultDigest,
    string SourceSnapshotId,
    ContentDigest SourceSnapshotRecordDigest,
    string TargetKind,
    string TargetId,
    ContentDigest TargetDigest,
    string PolicyId,
    string PolicyVersion,
    ContentDigest PolicyDigest,
    string ActionType,
    string ReasonCode,
    string? Rationale,
    string ActorId,
    string ActorRole,
    string? SupersedesDecisionId,
    ContentDigest? SupersedesDecisionDigest,
    ContentDigest? RequestDigest = null);

public sealed class VerifiedDeduplicationReviewCommand
{
    internal VerifiedDeduplicationReviewCommand(UnverifiedDeduplicationReviewCommand material, ContentDigest digest, DigestEnvelope envelope)
    {
        Material = material with { RequestDigest = digest };
        RequestDigest = digest;
        DigestEnvelope = envelope;
        var hex = digest.ToString()["sha256:".Length..];
        RequestId = $"request-{hex}";
        DecisionId = $"decision-{hex}";
    }

    public UnverifiedDeduplicationReviewCommand Material { get; }
    public string RequestId { get; }
    public string DecisionId { get; }
    public ContentDigest RequestDigest { get; }
    public DigestEnvelope DigestEnvelope { get; }
}

public static class DeduplicationReviewCommand
{
    public static VerifiedDeduplicationReviewCommand Create(
        UnverifiedDeduplicationReviewCommand input,
        VerifiedDeduplicationAuthorityPolicy policy,
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedDeduplicationAuthorityReviewTargetDigest target,
        ContentDigest expectedActiveDecisionSetDigest,
        string expectedAuthorityGenerationId,
        ContentDigest expectedAuthorityManifestDigest,
        string expectedSnapshotId,
        ContentDigest expectedSnapshotRecordDigest) =>
        Verify(input with
        {
            SchemaId = DeduplicationReviewCommandConstants.SchemaId,
            SchemaVersion = DeduplicationReviewCommandConstants.SchemaVersion,
            RequestDigest = null
        }, policy, sourceResult, target, expectedActiveDecisionSetDigest, expectedAuthorityGenerationId,
            expectedAuthorityManifestDigest, expectedSnapshotId, expectedSnapshotRecordDigest, rehydrate: false);

    public static VerifiedDeduplicationReviewCommand Rehydrate(
        UnverifiedDeduplicationReviewCommand input,
        VerifiedDeduplicationAuthorityPolicy policy,
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedDeduplicationAuthorityReviewTargetDigest target,
        ContentDigest expectedActiveDecisionSetDigest,
        string expectedAuthorityGenerationId,
        ContentDigest expectedAuthorityManifestDigest,
        string expectedSnapshotId,
        ContentDigest expectedSnapshotRecordDigest) =>
        Verify(input, policy, sourceResult, target, expectedActiveDecisionSetDigest, expectedAuthorityGenerationId,
            expectedAuthorityManifestDigest, expectedSnapshotId, expectedSnapshotRecordDigest, rehydrate: true);

    public static UnverifiedDeduplicationAuthorityDecision BuildDecisionMaterial(
        VerifiedDeduplicationReviewCommand command,
        VerifiedDeduplicationAuthorityReviewTargetDigest target)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(target);
        var value = command.Material;
        var invalidations = new List<DeduplicationAuthorityDecisionInvalidationEffect>
        {
            new(DeduplicationDecisionConstants.InvalidationSnapshotKind, value.SourceSnapshotId, value.SourceSnapshotRecordDigest)
        };
        if (value.SupersedesDecisionId is not null)
        {
            invalidations.Add(new DeduplicationAuthorityDecisionInvalidationEffect(
                DeduplicationDecisionConstants.InvalidationDecisionKind,
                value.SupersedesDecisionId,
                value.SupersedesDecisionDigest!.Value));
        }

        return new UnverifiedDeduplicationAuthorityDecision(
            DeduplicationDecisionConstants.SchemaId,
            DeduplicationDecisionConstants.SchemaVersion,
            command.DecisionId,
            value.ActionType,
            value.PolicyId,
            value.PolicyVersion,
            value.TargetKind,
            value.TargetId,
            value.TargetDigest,
            value.SourceResultId,
            value.SourceResultDigest,
            value.SourceSnapshotId,
            value.SourceSnapshotRecordDigest,
            target.Evidence.Select(evidence => new DeduplicationAuthorityDecisionEvidenceReference(
                evidence.Kind.ToString(), evidence.EvidenceId, DigestScope.CanonicalJsonRecord.Value,
                DeduplicationAuthorityDigests.CreateEvidenceDigestMaterial(evidence).EvidenceDigest)).ToArray(),
            value.ActorId,
            value.ActorRole,
            value.PolicyId,
            DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind,
            value.PolicyDigest,
            value.Rationale,
            value.ReasonCode,
            DateTimeOffset.UnixEpoch,
            value.SupersedesDecisionId,
            invalidations,
            null);
    }

    private static VerifiedDeduplicationReviewCommand Verify(
        UnverifiedDeduplicationReviewCommand input,
        VerifiedDeduplicationAuthorityPolicy policy,
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedDeduplicationAuthorityReviewTargetDigest target,
        ContentDigest expectedActiveDecisionSetDigest,
        string expectedAuthorityGenerationId,
        ContentDigest expectedAuthorityManifestDigest,
        string expectedSnapshotId,
        ContentDigest expectedSnapshotRecordDigest,
        bool rehydrate)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(sourceResult);
        ArgumentNullException.ThrowIfNull(target);
        if (!string.Equals(input.SchemaId, DeduplicationReviewCommandConstants.SchemaId, StringComparison.Ordinal) ||
            !string.Equals(input.SchemaVersion, DeduplicationReviewCommandConstants.SchemaVersion, StringComparison.Ordinal))
        {
            throw Error(DeduplicationReviewCommandErrorCodes.InvalidCommand, "Review command schema is invalid.");
        }

        var material = Normalize(input, rehydrate);
        if (!string.Equals(material.AuthorityGenerationId, expectedAuthorityGenerationId, StringComparison.Ordinal) ||
            material.AuthorityGenerationManifestDigest != expectedAuthorityManifestDigest ||
            material.ActiveDecisionSetDigest != expectedActiveDecisionSetDigest ||
            !string.Equals(material.SourceSnapshotId, expectedSnapshotId, StringComparison.Ordinal) ||
            material.SourceSnapshotRecordDigest != expectedSnapshotRecordDigest ||
            !string.Equals(material.SourceResultId, sourceResult.Result.ResultId, StringComparison.Ordinal) ||
            material.SourceResultDigest != sourceResult.ResultDigest ||
            !string.Equals(material.TargetKind, target.TargetKind, StringComparison.Ordinal) ||
            !string.Equals(material.TargetId, target.TargetId, StringComparison.Ordinal) ||
            material.TargetDigest != target.TargetDigest ||
            !string.Equals(material.PolicyId, policy.PolicyId, StringComparison.Ordinal) ||
            !string.Equals(material.PolicyVersion, policy.PolicyVersion, StringComparison.Ordinal) ||
            material.PolicyDigest != policy.PolicyDigest)
        {
            throw Error(DeduplicationReviewCommandErrorCodes.StaleCommandBinding, "Review command binding is stale.");
        }

        if (!policy.ContainsAuthorizedActor(material.ActorId, material.ActorRole))
        {
            throw Error(DeduplicationReviewCommandErrorCodes.UnauthorizedActor, "Review command actor-role pair is not authorized.");
        }

        if (!policy.AllowedActions.Contains(material.ActionType, StringComparer.Ordinal) ||
            !policy.ReasonCodesForAction(material.ActionType).Contains(material.ReasonCode, StringComparer.Ordinal) ||
            policy.RequiresRationale && string.IsNullOrWhiteSpace(material.Rationale))
        {
            throw Error(DeduplicationReviewCommandErrorCodes.InvalidCommand, "Review action, reason, or rationale is not allowed by policy.");
        }

        var envelope = new DigestEnvelope(DigestScope.CanonicalJsonRecord, DeduplicationReviewCommandConstants.SchemaId,
            DeduplicationReviewCommandConstants.SchemaVersion, BuildContent(material));
        var digest = envelope.ComputeDigest();
        if (rehydrate && material.RequestDigest != digest)
        {
            throw Error(DeduplicationReviewCommandErrorCodes.InvalidCommand, "Review command digest does not match persisted material.");
        }

        return new VerifiedDeduplicationReviewCommand(material, digest, envelope);
    }

    private static UnverifiedDeduplicationReviewCommand Normalize(UnverifiedDeduplicationReviewCommand input, bool canonical)
    {
        string Text(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || canonical && !value.IsNormalized(NormalizationForm.FormC))
            {
                throw Error(canonical ? DeduplicationReviewCommandErrorCodes.NonCanonicalCommand : DeduplicationReviewCommandErrorCodes.InvalidCommand, $"{name} is invalid.");
            }
            return value;
        }

        var hasSupersedesId = !string.IsNullOrWhiteSpace(input.SupersedesDecisionId);
        var hasSupersedesDigest = input.SupersedesDecisionDigest is { IsValid: true };
        if (hasSupersedesId != hasSupersedesDigest)
        {
            throw Error(DeduplicationReviewCommandErrorCodes.InvalidCommand, "Supersession id and digest are required together.");
        }

        return input with
        {
            AuthorityGenerationId = Text(input.AuthorityGenerationId, nameof(input.AuthorityGenerationId)),
            SourceResultId = Text(input.SourceResultId, nameof(input.SourceResultId)),
            SourceSnapshotId = Text(input.SourceSnapshotId, nameof(input.SourceSnapshotId)),
            TargetKind = Text(input.TargetKind, nameof(input.TargetKind)),
            TargetId = Text(input.TargetId, nameof(input.TargetId)),
            PolicyId = Text(input.PolicyId, nameof(input.PolicyId)),
            PolicyVersion = Text(input.PolicyVersion, nameof(input.PolicyVersion)),
            ActionType = Text(input.ActionType, nameof(input.ActionType)),
            ReasonCode = Text(input.ReasonCode, nameof(input.ReasonCode)),
            ActorId = Text(input.ActorId, nameof(input.ActorId)),
            ActorRole = Text(input.ActorRole, nameof(input.ActorRole)),
            Rationale = string.IsNullOrWhiteSpace(input.Rationale) ? null : Text(input.Rationale, nameof(input.Rationale)),
            SupersedesDecisionId = hasSupersedesId ? Text(input.SupersedesDecisionId!, nameof(input.SupersedesDecisionId)) : null
        };
    }

    private static CanonicalJsonObject BuildContent(UnverifiedDeduplicationReviewCommand value)
    {
        var content = new CanonicalJsonObject()
            .Add("schema_id", DeduplicationReviewCommandConstants.SchemaId)
            .Add("schema_version", DeduplicationReviewCommandConstants.SchemaVersion)
            .Add("authority_generation_id", value.AuthorityGenerationId)
            .Add("authority_generation_manifest_digest", value.AuthorityGenerationManifestDigest.ToString())
            .Add("active_decision_set_digest", value.ActiveDecisionSetDigest.ToString())
            .Add("source_result_id", value.SourceResultId)
            .Add("source_result_digest", value.SourceResultDigest.ToString())
            .Add("source_snapshot_id", value.SourceSnapshotId)
            .Add("source_snapshot_record_digest", value.SourceSnapshotRecordDigest.ToString())
            .Add("target_kind", value.TargetKind)
            .Add("target_id", value.TargetId)
            .Add("target_digest", value.TargetDigest.ToString())
            .Add("policy_id", value.PolicyId)
            .Add("policy_version", value.PolicyVersion)
            .Add("policy_digest", value.PolicyDigest.ToString())
            .Add("action_type", value.ActionType)
            .Add("reason_code", value.ReasonCode)
            .Add("actor_id", value.ActorId)
            .Add("actor_role", value.ActorRole);
        if (value.Rationale is not null) content.Add("rationale", value.Rationale);
        if (value.SupersedesDecisionId is not null)
        {
            content.Add("supersedes_decision_id", value.SupersedesDecisionId)
                .Add("supersedes_decision_digest", value.SupersedesDecisionDigest!.Value.ToString());
        }
        return content;
    }

    private static DeduplicationAuthorityException Error(string category, string message) => new(category, message);
}
