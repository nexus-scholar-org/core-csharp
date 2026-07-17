using System.Globalization;
using NexusScholar.Kernel;
using NexusScholar.ResearchWorkspace;

namespace NexusScholar.Desktop.AppServices;

public sealed partial class DesktopWorkspaceCommandFacade
{
    private static readonly string[] FullTextIntakeNonClaims =
    {
        "human-actor-required",
        "no-ai",
        "no-network",
        "no-live-provider"
    };

    private static readonly string[] FullTextReviewNonClaims =
    {
        "human-actor-required",
        "no-ai",
        "no-network",
        "no-live-provider"
    };

    public DesktopFullTextIntakePreviewResult PreviewFullTextIntake(
        DesktopFullTextIntakeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var result = ResearchWorkspaceFullTextWorkflow.PreviewIntake(new ResearchWorkspaceFullTextIntakeRequest(
                request.WorkingDirectory,
                request.CandidateId,
                request.LocalPath,
                request.ArtifactKind,
                request.MediaType,
                request.ActorId,
                request.ActorKind,
                request.OccurredAt,
                request.MaximumBytes,
                request.ExpectedSupersededManifestDigest));

            if (!result.IsReady)
            {
                return new DesktopFullTextIntakePreviewResult(
                    MapFullTextStatus(result.Status), result.Message, null);
            }

            var preview = new DesktopFullTextIntakePreview(
                result.WorkspaceDirectory,
                result.WorkspaceId!,
                result.ExpectedProjectRevision!.Value,
                result.ScreeningAuthorityManifestDigest!,
                result.ScreeningConductManifestDigest!,
                result.ScreeningHandoffDigest!,
                result.CandidateId!,
                result.LocalPath!,
                result.ArtifactKind!,
                result.MediaType!,
                result.ActorId!,
                result.ActorKind!,
                result.OccurredAt,
                result.MaximumBytes,
                result.ExpectedSupersededManifestDigest,
                result.AdmissionDigest!,
                result.InputDigest!,
                result.AcquisitionDigest!,
                result.ArtifactEvidenceDigest!,
                result.RawArtifactDigest!,
                result.ExtractionAttemptDigest!,
                result.ExtractionStatus,
                result.ResultingGenerationId!,
                result.ExpectedEffects,
                FullTextIntakeNonClaims,
                result.ConfirmationToken!,
                CreateFullTextIntakeConfirmationToken(new ResearchWorkspaceFullTextIntakePreview(
                    result.Status,
                    result.ExitCode,
                    result.Message,
                    result.WorkspaceDirectory,
                    result.WorkspaceId,
                    result.ExpectedProjectRevision,
                    result.ScreeningAuthorityManifestDigest,
                    result.ScreeningConductManifestDigest,
                    result.ScreeningHandoffDigest,
                    result.CandidateId,
                    result.LocalPath,
                    result.ArtifactKind,
                    result.MediaType,
                    result.ActorId,
                    result.ActorKind,
                    result.OccurredAt,
                    result.MaximumBytes,
                    result.ExpectedSupersededManifestDigest,
                    result.AdmissionDigest,
                    result.InputDigest,
                    result.AcquisitionDigest,
                    result.ArtifactEvidenceDigest,
                    result.RawArtifactDigest,
                    result.ExtractionAttemptDigest,
                    result.ExtractionStatus,
                    result.ResultingGenerationId,
                    result.ExpectedEffects,
                    result.ConfirmationToken)));
            return new DesktopFullTextIntakePreviewResult(DesktopWorkspaceCommandStatus.Ready, result.Message, preview);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            return new DesktopFullTextIntakePreviewResult(
                DesktopWorkspaceCommandStatus.Failed,
                "local Full Text preview failed to materialize a stable payload.",
                null);
        }
    }

    public DesktopFullTextIntakeCommandResult ExecuteFullTextIntake(
        DesktopFullTextIntakePreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var operationPreview = FullTextIntakeOperationPreview(preview);
        string expectedToken;
        try
        {
            expectedToken = CreateFullTextIntakeConfirmationToken(preview);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            return FullTextFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-fulltext-intake: preview material is incomplete.");
        }

        if (!string.Equals(preview.ConfirmationToken, expectedToken, StringComparison.Ordinal))
        {
            return FullTextFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-fulltext-intake: preview material or token changed.");
        }

        var committed = ResearchWorkspaceFullTextWorkflow.CommitIntake(operationPreview);
        if (!committed.Completed)
        {
            return FullTextFailure(MapFullTextStatus(committed.Status), committed.Message);
        }

        return new DesktopFullTextIntakeCommandResult(
            DesktopWorkspaceCommandStatus.Succeeded,
            committed.Message,
            committed.CandidateId,
            committed.Project?.CurrentFullTextGenerationId ?? committed.GenerationId,
            committed.RawArtifactDigest,
            committed.AlreadyApplied,
            SafeBuild(preview.WorkspaceDirectory));
    }

    public DesktopFullTextReviewPreviewResult PreviewFullTextReview(
        DesktopFullTextReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = ResearchWorkspaceFullTextWorkflow.PreviewReview(new ResearchWorkspaceFullTextReviewRequest(
            request.WorkingDirectory,
            request.Verdict,
            request.ActorId,
            request.ActorKind,
            request.ActorRole,
            request.Rationale,
            request.InclusionCriteria,
            request.ExclusionCriteria,
            request.ExclusionReasonCode,
            request.SelectedExclusionReasonCode,
            request.OccurredAt,
            request.CandidateId));
        if (!result.IsReady)
        {
            return new DesktopFullTextReviewPreviewResult(MapFullTextStatus(result.Status), result.Message, null);
        }

        var preview = new DesktopFullTextReviewPreview(
            result.WorkspaceDirectory,
            result.WorkspaceId!,
            result.ExpectedProjectRevision!.Value,
            result.ScreeningAuthorityManifestDigest!,
            result.ScreeningConductManifestDigest!,
            result.ScreeningHandoffDigest!,
            result.FullTextGenerationId!,
            result.FullTextManifestDigest!,
            result.CandidateId!,
            result.AdmissionDigest!,
            result.RawArtifactDigest!,
            result.ExtractionAttemptDigest!,
            result.ExtractionStatus,
            result.CriteriaDigest!,
            result.PolicyDigest!,
            result.HeaderDigest!,
            result.DecisionDigest!,
            result.ResultingHeadDigest!,
            result.Verdict!,
            result.ActorId!,
            result.ActorKind!,
            result.ActorRole!,
            result.Rationale!,
            result.InclusionCriteria!,
            result.ExclusionCriteria!,
            result.ExclusionReasonCode!,
            result.SelectedExclusionReasonCode,
            result.OccurredAt,
            result.ExpectedEffects,
            FullTextReviewNonClaims,
            result.ConfirmationToken!,
            CreateFullTextReviewConfirmationToken(new ResearchWorkspaceFullTextReviewPreview(
                result.Status,
                result.ExitCode,
                result.Message,
                result.WorkspaceDirectory,
                result.WorkspaceId,
                result.ExpectedProjectRevision,
                result.ScreeningAuthorityManifestDigest,
                result.ScreeningConductManifestDigest,
                result.ScreeningHandoffDigest,
                result.FullTextGenerationId,
                result.FullTextManifestDigest,
                result.CandidateId,
                result.AdmissionDigest,
                result.RawArtifactDigest,
                result.ExtractionAttemptDigest,
                result.ExtractionStatus,
                result.CriteriaDigest,
                result.PolicyDigest,
                result.HeaderDigest,
                result.DecisionDigest,
                result.ResultingHeadDigest,
                result.Verdict,
                result.ActorId,
                result.ActorKind,
                result.ActorRole,
                result.Rationale,
                result.InclusionCriteria,
                result.ExclusionCriteria,
                result.ExclusionReasonCode,
                result.SelectedExclusionReasonCode,
                result.OccurredAt,
                result.ExpectedEffects,
                result.ConfirmationToken)));
        return new DesktopFullTextReviewPreviewResult(DesktopWorkspaceCommandStatus.Ready, result.Message, preview);
    }

    public DesktopFullTextReviewCommandResult ExecuteFullTextReview(
        DesktopFullTextReviewPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var operationPreview = FullTextReviewOperationPreview(preview);
        string expectedToken;
        try
        {
            expectedToken = CreateFullTextReviewConfirmationToken(preview);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            return FullTextReviewFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-fulltext-review: preview material is incomplete.");
        }

        if (!string.Equals(preview.ConfirmationToken, expectedToken, StringComparison.Ordinal))
        {
            return FullTextReviewFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-fulltext-review: preview material or token changed.");
        }

        var committed = ResearchWorkspaceFullTextWorkflow.CommitReview(operationPreview);
        if (!committed.Completed)
        {
            return FullTextReviewFailure(MapFullTextStatus(committed.Status), committed.Message);
        }

        return new DesktopFullTextReviewCommandResult(
            DesktopWorkspaceCommandStatus.Succeeded,
            committed.Message,
            committed.CandidateId,
            committed.DecisionDigest,
            committed.HeadDigest,
            committed.HandoffReady,
            committed.AlreadyApplied,
            SafeBuild(preview.WorkspaceDirectory));
    }

    internal static string CreateFullTextIntakeConfirmationToken(
        ResearchWorkspaceFullTextIntakePreview value) =>
        FullTextIntakeConfirmationToken(value, value.ExpectedEffects, FullTextIntakeNonClaims);

    internal static string CreateFullTextIntakeConfirmationToken(
        DesktopFullTextIntakePreview value)
    {
        var operation = FullTextIntakeOperationPreview(value);
        return FullTextIntakeConfirmationToken(operation, operation.ExpectedEffects, value.NonClaims);
    }

    internal static string CreateFullTextReviewConfirmationToken(
        ResearchWorkspaceFullTextReviewPreview value) =>
        FullTextReviewConfirmationToken(value, value.ExpectedEffects, FullTextReviewNonClaims);

    internal static string CreateFullTextReviewConfirmationToken(
        DesktopFullTextReviewPreview value)
    {
        var operation = FullTextReviewOperationPreview(value);
        return FullTextReviewConfirmationToken(operation, operation.ExpectedEffects, value.NonClaims);
    }

    private static ResearchWorkspaceFullTextIntakePreview FullTextIntakeOperationPreview(
        DesktopFullTextIntakePreview value) => new(
        ResearchWorkspaceOperationStatus.Succeeded,
        ResearchWorkspaceExitCodes.Success,
        "Review the exact local Full Text intake effects before confirmation.",
        value.WorkspaceDirectory,
        value.WorkspaceId,
        value.ExpectedProjectRevision,
        value.ScreeningAuthorityManifestDigest,
        value.ScreeningConductManifestDigest,
        value.ScreeningHandoffDigest,
        value.CandidateId,
        value.LocalPath,
        value.ArtifactKind,
        value.MediaType,
        value.ActorId,
        value.ActorKind,
        value.OccurredAt,
        value.MaximumBytes,
        value.ExpectedSupersededManifestDigest,
        value.AdmissionDigest,
        value.InputDigest,
        value.AcquisitionDigest,
        value.ArtifactEvidenceDigest,
        value.RawArtifactDigest,
        value.ExtractionAttemptDigest,
        value.ExtractionStatus,
        value.ResultingGenerationId,
        value.ExpectedEffects,
        value.OperationConfirmationToken);

    private static ResearchWorkspaceFullTextReviewPreview FullTextReviewOperationPreview(
        DesktopFullTextReviewPreview value) => new(
        ResearchWorkspaceOperationStatus.Succeeded,
        ResearchWorkspaceExitCodes.Success,
        "Review the exact Full Text Screening effects before confirmation.",
        value.WorkspaceDirectory,
        value.WorkspaceId,
        value.ExpectedProjectRevision,
        value.ScreeningAuthorityManifestDigest,
        value.ScreeningConductManifestDigest,
        value.ScreeningHandoffDigest,
        value.FullTextGenerationId,
        value.FullTextManifestDigest,
        value.CandidateId,
        value.AdmissionDigest,
        value.RawArtifactDigest,
        value.ExtractionAttemptDigest,
        value.ExtractionStatus,
        value.CriteriaDigest,
        value.PolicyDigest,
        value.HeaderDigest,
        value.DecisionDigest,
        value.ResultingHeadDigest,
        value.Verdict,
        value.ActorId,
        value.ActorKind,
        value.ActorRole,
        value.Rationale,
        value.InclusionCriteria,
        value.ExclusionCriteria,
        value.ExclusionReasonCode,
        value.SelectedExclusionReasonCode,
        value.OccurredAt,
        value.ExpectedEffects,
        value.OperationConfirmationToken);

    private static string FullTextIntakeConfirmationToken(
        ResearchWorkspaceFullTextIntakePreview value,
        IReadOnlyList<string> effects,
        IReadOnlyList<string> nonClaims)
    {
        CanonicalJsonValue Optional(string? value) =>
            value is null ? CanonicalJsonValue.Null() : CanonicalJsonValue.From(value);
        var material = new CanonicalJsonObject()
            .Add("schema", "nexus.desktop.fulltext-intake-preview")
            .Add("schema_version", "1.0.0")
            .Add("operation", "fulltext-intake")
            .Add("workspace_directory", value.WorkspaceDirectory)
            .Add("workspace_id", value.WorkspaceId is null ? CanonicalJsonValue.Null() : CanonicalJsonValue.From(value.WorkspaceId))
            .Add("expected_project_revision", value.ExpectedProjectRevision ?? 0L)
            .Add("screening_authority_manifest_digest", Optional(value.ScreeningAuthorityManifestDigest))
            .Add("screening_conduct_manifest_digest", Optional(value.ScreeningConductManifestDigest))
            .Add("screening_handoff_digest", Optional(value.ScreeningHandoffDigest))
            .Add("candidate_id", value.CandidateId!)
            .Add("local_path", value.LocalPath!)
            .Add("artifact_kind", value.ArtifactKind!)
            .Add("media_type", value.MediaType!)
            .Add("actor_id", value.ActorId!)
            .Add("actor_kind", value.ActorKind!)
            .Add("occurred_at", value.OccurredAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))
            .Add("maximum_bytes", value.MaximumBytes)
            .Add("expected_superseded_manifest_digest",
                value.ExpectedSupersededManifestDigest is null
                    ? CanonicalJsonValue.Null()
                    : CanonicalJsonValue.From(value.ExpectedSupersededManifestDigest))
            .Add("admission_digest", value.AdmissionDigest!)
            .Add("input_digest", value.InputDigest!)
            .Add("acquisition_digest", value.AcquisitionDigest!)
            .Add("artifact_evidence_digest", value.ArtifactEvidenceDigest!)
            .Add("raw_artifact_digest", value.RawArtifactDigest!)
            .Add("extraction_attempt_digest", value.ExtractionAttemptDigest!)
            .Add("extraction_status", value.ExtractionStatus is null
                ? CanonicalJsonValue.Null()
                : CanonicalJsonValue.From(value.ExtractionStatus))
            .Add("resulting_generation_id", value.ResultingGenerationId!)
            .Add("operation_confirmation_token", value.ConfirmationToken!)
            .Add("expected_effects", new CanonicalJsonArray(effects.Select(CanonicalJsonValue.From)))
            .Add("non_claims", new CanonicalJsonArray(nonClaims.Select(CanonicalJsonValue.From)));
        return ContentDigest.Sha256CanonicalJson(material).ToString();
    }

    private static string FullTextReviewConfirmationToken(
        ResearchWorkspaceFullTextReviewPreview value,
        IReadOnlyList<string> effects,
        IReadOnlyList<string> nonClaims)
    {
        CanonicalJsonValue Optional(string? value) =>
            value is null ? CanonicalJsonValue.Null() : CanonicalJsonValue.From(value);
        var material = new CanonicalJsonObject()
            .Add("schema", "nexus.desktop.fulltext-review-preview")
            .Add("schema_version", "1.0.0")
            .Add("operation", "fulltext-review")
            .Add("workspace_directory", value.WorkspaceDirectory)
            .Add("workspace_id", value.WorkspaceId!)
            .Add("expected_project_revision", value.ExpectedProjectRevision!.Value)
            .Add("screening_authority_manifest_digest", value.ScreeningAuthorityManifestDigest!)
            .Add("screening_conduct_manifest_digest", value.ScreeningConductManifestDigest!)
            .Add("screening_handoff_digest", value.ScreeningHandoffDigest!)
            .Add("fulltext_generation_id", value.FullTextGenerationId!)
            .Add("fulltext_manifest_digest", value.FullTextManifestDigest!)
            .Add("candidate_id", value.CandidateId!)
            .Add("admission_digest", value.AdmissionDigest!)
            .Add("raw_artifact_digest", value.RawArtifactDigest!)
            .Add("extraction_attempt_digest", value.ExtractionAttemptDigest!)
            .Add("extraction_status", Optional(value.ExtractionStatus))
            .Add("criteria_digest", value.CriteriaDigest!)
            .Add("policy_digest", value.PolicyDigest!)
            .Add("header_digest", value.HeaderDigest!)
            .Add("decision_digest", value.DecisionDigest!)
            .Add("resulting_head_digest", value.ResultingHeadDigest!)
            .Add("verdict", value.Verdict!)
            .Add("actor_id", value.ActorId!)
            .Add("actor_kind", value.ActorKind!)
            .Add("actor_role", value.ActorRole!)
            .Add("rationale", value.Rationale!)
            .Add("inclusion_criteria", value.InclusionCriteria!)
            .Add("exclusion_criteria", value.ExclusionCriteria!)
            .Add("exclusion_reason_code", value.ExclusionReasonCode!)
            .Add("selected_exclusion_reason_code", Optional(value.SelectedExclusionReasonCode))
            .Add("occurred_at", value.OccurredAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))
            .Add("operation_confirmation_token", value.ConfirmationToken!)
            .Add("expected_effects", new CanonicalJsonArray(effects.Select(CanonicalJsonValue.From)))
            .Add("non_claims", new CanonicalJsonArray(nonClaims.Select(CanonicalJsonValue.From)));
        return ContentDigest.Sha256CanonicalJson(material).ToString();
    }

    private static DesktopWorkspaceCommandStatus MapFullTextStatus(
        ResearchWorkspaceOperationStatus status) => status switch
        {
            ResearchWorkspaceOperationStatus.Succeeded => DesktopWorkspaceCommandStatus.Succeeded,
            ResearchWorkspaceOperationStatus.Stale => DesktopWorkspaceCommandStatus.Stale,
            ResearchWorkspaceOperationStatus.RecoveryRequired => DesktopWorkspaceCommandStatus.RecoveryRequired,
            _ => DesktopWorkspaceCommandStatus.Failed
        };

    private static DesktopFullTextIntakeCommandResult FullTextFailure(
        DesktopWorkspaceCommandStatus status,
        string message) => new(status, message, null, null, null, false, null);

    private static DesktopFullTextReviewCommandResult FullTextReviewFailure(
        DesktopWorkspaceCommandStatus status,
        string message) => new(status, message, null, null, null, false, false, null);
}
