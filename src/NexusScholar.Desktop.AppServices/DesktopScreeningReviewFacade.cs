using System.Globalization;
using NexusScholar.Kernel;
using NexusScholar.ResearchWorkspace;

namespace NexusScholar.Desktop.AppServices;

public sealed partial class DesktopWorkspaceCommandFacade
{
    private static readonly string[] ScreeningReviewNonClaims =
    {
        "human-actor-required",
        "not-authentication",
        "no-ai",
        "no-network"
    };

    public DesktopScreeningReviewQueueResult LoadScreeningReviewQueue(string workspaceDirectory)
    {
        var result = ResearchWorkspaceScreeningReview.Inspect(workspaceDirectory);
        if (!result.Completed || result.WorkspaceId is null ||
            result.ProjectRevision is null || result.AuthorityPackageGenerationId is null ||
            result.AuthorityPackageManifestDigest is null || result.ConductGenerationId is null ||
            result.ConductManifestDigest is null || result.PolicyId is null ||
            result.PolicyDigest is null || result.CriteriaId is null || result.CriteriaDigest is null)
        {
            return new DesktopScreeningReviewQueueResult(MapScreening(result.Status), result.Message, null);
        }

        var queue = Queue(result);
        var status = queue.Targets.Count == 0
            ? DesktopWorkspaceCommandStatus.Succeeded
            : DesktopWorkspaceCommandStatus.Attention;
        return new DesktopScreeningReviewQueueResult(status, result.Message, queue);
    }

    public DesktopScreeningReviewPreviewResult PreviewScreeningReview(DesktopScreeningReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = ResearchWorkspaceScreeningReview.Preview(new ResearchWorkspaceScreeningReviewRequest(
            request.WorkspaceDirectory,
            request.CandidateId,
            request.DecisionKind,
            request.Verdict,
            request.ActorId,
            request.ActorKind,
            request.ActorRole,
            request.Rationale,
            request.ExclusionReasonCode,
            request.OccurredAt));

        if (!result.IsReady)
        {
            return new DesktopScreeningReviewPreviewResult(MapScreening(result.Status), result.Message, null);
        }

        var effects = result.ExpectedEffects;
        var preview = new DesktopScreeningReviewPreview(
            result.WorkspaceDirectory,
            result.WorkspaceId!,
            result.ExpectedProjectRevision!.Value,
            result.AuthorityPackageGenerationId!,
            result.AuthorityPackageManifestDigest!,
            result.SourceResultDigest!,
            result.SourceSnapshotRecordDigest!,
            result.DecisionSetDigest!,
            result.ProtocolContentDigest!,
            result.CriteriaDigest!,
            result.CorpusBindingDigest!,
            result.ConductGenerationId!,
            result.ConductManifestDigest!,
            result.PolicyId!,
            result.PolicyDigest!,
            result.HeaderDigest!,
            result.PriorHeadDigest!,
            result.ResultingHeadDigest!,
            result.PriorEntryCount,
            result.CandidateId!,
            result.TargetDigest!,
            result.DecisionKind!,
            result.Verdict!,
            result.ActorId!,
            result.ActorKind!,
            result.ActorRole!,
            result.Rationale!,
            result.ExclusionReasonCode,
            result.SourceDecisionDigests,
            result.OccurredAt,
            result.DecisionId!,
            result.DecisionDigest!,
            effects,
            ScreeningReviewNonClaims,
            ConfirmationToken(result, effects, ScreeningReviewNonClaims));
        return new DesktopScreeningReviewPreviewResult(DesktopWorkspaceCommandStatus.Ready, result.Message, preview);
    }

    public DesktopScreeningReviewCommandResult ExecuteScreeningReview(DesktopScreeningReviewPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var operationPreview = OperationPreview(preview);
        string expectedToken;
        try
        {
            expectedToken = CreateScreeningConfirmationToken(preview);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            return ScreeningFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-screening-review: preview material is incomplete.");
        }
        if (!string.Equals(preview.ConfirmationToken, expectedToken, StringComparison.Ordinal))
        {
            return ScreeningFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-screening-review: preview material or token changed.");
        }

        var committed = ResearchWorkspaceScreeningReview.Commit(operationPreview);
        if (!committed.Completed)
        {
            return ScreeningFailure(MapScreening(committed.Status), committed.Message);
        }

        var queue = LoadScreeningReviewQueue(preview.WorkspaceDirectory).Queue;
        var overview = SafeBuild(preview.WorkspaceDirectory);
        return new DesktopScreeningReviewCommandResult(
            DesktopWorkspaceCommandStatus.Succeeded,
            committed.Message,
            committed.DecisionId,
            committed.HeadDigest,
            committed.AlreadyApplied,
            overview,
            queue);
    }

    private static ResearchWorkspaceScreeningReviewPreview OperationPreview(DesktopScreeningReviewPreview value) => new(
        ResearchWorkspaceOperationStatus.Succeeded,
        ResearchWorkspaceExitCodes.Success,
        "Review the exact screening authority effects before confirmation.",
        value.WorkspaceDirectory,
        value.WorkspaceId,
        value.ExpectedProjectRevision,
        value.AuthorityPackageGenerationId,
        value.AuthorityPackageManifestDigest,
        value.SourceResultDigest,
        value.SourceSnapshotRecordDigest,
        value.DecisionSetDigest,
        value.ProtocolContentDigest,
        value.CriteriaDigest,
        value.CorpusBindingDigest,
        value.ConductGenerationId,
        value.ConductManifestDigest,
        value.PolicyId,
        value.PolicyDigest,
        value.HeaderDigest,
        value.PriorHeadDigest,
        value.ResultingHeadDigest,
        value.PriorEntryCount,
        value.CandidateId,
        value.TargetDigest,
        value.DecisionKind,
        value.Verdict,
        value.ActorId,
        value.ActorKind,
        value.ActorRole,
        value.Rationale,
        value.ExclusionReasonCode,
        value.SourceDecisionDigests,
        value.OccurredAt,
        value.DecisionId,
        value.DecisionDigest,
        value.ExpectedEffects,
        value.ConfirmationToken);

    internal static string CreateScreeningConfirmationToken(
        DesktopScreeningReviewPreview value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var operation = OperationPreview(value);
        return ConfirmationToken(operation, operation.ExpectedEffects, value.NonClaims);
    }

    private static string ConfirmationToken(
        ResearchWorkspaceScreeningReviewPreview value,
        IReadOnlyList<string> effects,
        IReadOnlyList<string> nonClaims)
    {
        CanonicalJsonValue Optional(string? text) => text is null ? CanonicalJsonValue.Null() : CanonicalJsonValue.From(text);
        var material = new CanonicalJsonObject()
            .Add("schema", "nexus.desktop.screening-review-preview")
            .Add("schema_version", "1.0.0")
            .Add("workspace_directory", value.WorkspaceDirectory)
            .Add("workspace_id", value.WorkspaceId!)
            .Add("expected_project_revision", value.ExpectedProjectRevision!.Value)
            .Add("authority_package_generation_id", value.AuthorityPackageGenerationId!)
            .Add("authority_package_manifest_digest", value.AuthorityPackageManifestDigest!)
            .Add("source_result_digest", value.SourceResultDigest!)
            .Add("source_snapshot_record_digest", value.SourceSnapshotRecordDigest!)
            .Add("decision_set_digest", value.DecisionSetDigest!)
            .Add("protocol_content_digest", value.ProtocolContentDigest!)
            .Add("criteria_digest", value.CriteriaDigest!)
            .Add("corpus_binding_digest", value.CorpusBindingDigest!)
            .Add("conduct_generation_id", value.ConductGenerationId!)
            .Add("conduct_manifest_digest", value.ConductManifestDigest!)
            .Add("policy_id", value.PolicyId!)
            .Add("policy_digest", value.PolicyDigest!)
            .Add("header_digest", value.HeaderDigest!)
            .Add("prior_head_digest", value.PriorHeadDigest!)
            .Add("resulting_head_digest", value.ResultingHeadDigest!)
            .Add("prior_entry_count", value.PriorEntryCount)
            .Add("candidate_id", value.CandidateId!)
            .Add("target_digest", value.TargetDigest!)
            .Add("decision_kind", value.DecisionKind!)
            .Add("verdict", value.Verdict!)
            .Add("actor_id", value.ActorId!)
            .Add("actor_kind", value.ActorKind!)
            .Add("actor_role", value.ActorRole!)
            .Add("rationale", Optional(value.Rationale))
            .Add("exclusion_reason_code", Optional(value.ExclusionReasonCode))
            .Add("source_decision_digests", new CanonicalJsonArray(value.SourceDecisionDigests.Select(CanonicalJsonValue.From)))
            .Add("occurred_at", value.OccurredAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))
            .Add("decision_id", value.DecisionId!)
            .Add("decision_digest", value.DecisionDigest!)
            .Add("expected_effects", new CanonicalJsonArray(effects.Select(CanonicalJsonValue.From)))
            .Add("non_claims", new CanonicalJsonArray(nonClaims.Select(CanonicalJsonValue.From)));
        return ContentDigest.Sha256CanonicalJson(material).ToString();
    }

    private static DesktopScreeningReviewQueue Queue(ResearchWorkspaceScreeningReviewQueue value) => new(
        value.WorkspaceId!,
        value.ProjectRevision!.Value,
        value.AuthorityPackageGenerationId!,
        value.AuthorityPackageManifestDigest!,
        value.ConductGenerationId!,
        value.ConductManifestDigest!,
        value.PolicyId!,
        value.PolicyDigest!,
        value.CriteriaId!,
        value.CriteriaDigest!,
        value.RequiredReviewCount,
        value.AssignedActorRoles,
        value.AdjudicatorRoles,
        value.ExclusionReasons,
        value.HandoffReady,
        value.Targets.Select(target => new DesktopScreeningReviewTarget(
            target.CandidateId,
            target.TargetDigest,
            target.CurrentVerdict,
            target.ExclusionReasonCode,
            target.CurrentDecisions.Select(decision => new DesktopScreeningReviewDecisionSummary(
                decision.DecisionId,
                decision.DecisionDigest,
                decision.Kind,
                decision.Verdict,
                decision.ActorId,
                decision.ActorRole)).ToArray(),
            target.Conflicts.Select(conflict => new DesktopScreeningReviewConflictSummary(
                conflict.ConflictId,
                conflict.SourceDecisionDigests,
                conflict.Resolved)).ToArray())).ToArray());

    private static DesktopWorkspaceCommandStatus MapScreening(ResearchWorkspaceOperationStatus status) => status switch
    {
        ResearchWorkspaceOperationStatus.Succeeded => DesktopWorkspaceCommandStatus.Succeeded,
        ResearchWorkspaceOperationStatus.Stale => DesktopWorkspaceCommandStatus.Stale,
        ResearchWorkspaceOperationStatus.RecoveryRequired => DesktopWorkspaceCommandStatus.RecoveryRequired,
        _ => DesktopWorkspaceCommandStatus.Failed
    };

    private static DesktopScreeningReviewCommandResult ScreeningFailure(
        DesktopWorkspaceCommandStatus status,
        string message) => new(status, message, null, null, false, null, null);
}
