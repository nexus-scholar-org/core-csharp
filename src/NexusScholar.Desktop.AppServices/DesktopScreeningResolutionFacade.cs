using System.Globalization;
using NexusScholar.Kernel;
using NexusScholar.ResearchWorkspace;

namespace NexusScholar.Desktop.AppServices;

public sealed partial class DesktopWorkspaceCommandFacade
{
    private static readonly string[] ScreeningResolutionNonClaims =
    {
        "human-actor-required",
        "not-authentication",
        "no-ai",
        "no-network"
    };

    private static readonly string[] ScreeningHandoffNonClaims =
    {
        "human-actor-required",
        "not-authentication",
        "no-ai",
        "no-network"
    };

    public DesktopScreeningResolutionPreviewResult PreviewScreeningResolution(DesktopScreeningResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = ResearchWorkspaceScreeningResolution.Preview(new ResearchWorkspaceScreeningResolutionRequest(
            request.WorkspaceDirectory,
            request.CandidateId,
            request.DecisionKind,
            request.Verdict,
            request.ActorId,
            request.ActorKind,
            request.ActorRole,
            request.Rationale,
            request.ExclusionReasonCode,
            request.SupersedesDecisionDigest,
            request.ResolvedConflictId,
            request.SourceDecisionDigests,
            request.OccurredAt));
        if (!result.IsReady)
        {
            return new DesktopScreeningResolutionPreviewResult(MapScreeningResolutionStatus(result.Status), result.Message, null);
        }

        var effects = result.ExpectedEffects;
        var preview = new DesktopScreeningResolutionPreview(
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
            result.TargetSummaryDigest!,
            result.DecisionKind!,
            result.Verdict!,
            result.ActorId!,
            result.ActorKind!,
            result.ActorRole!,
            result.Rationale!,
            result.ExclusionReasonCode,
            result.SupersedesDecisionDigest,
            result.ResolvedConflictId,
            result.SourceDecisionDigests,
            result.OccurredAt,
            result.DecisionId!,
            result.DecisionDigest!,
            effects,
            ScreeningResolutionNonClaims,
            result.ConfirmationToken!,
            CreateScreeningResolutionConfirmationToken(result, effects, ScreeningResolutionNonClaims));
        return new DesktopScreeningResolutionPreviewResult(DesktopWorkspaceCommandStatus.Ready, result.Message, preview);
    }

    public DesktopScreeningResolutionCommandResult ExecuteScreeningResolution(DesktopScreeningResolutionPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var operationPreview = OperationPreview(preview);
        string expectedToken;
        try
        {
            expectedToken = CreateScreeningResolutionConfirmationToken(preview);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            return ScreeningResolutionFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-screening-resolution: preview material is incomplete.");
        }
        if (!string.Equals(preview.ConfirmationToken, expectedToken, StringComparison.Ordinal))
        {
            return ScreeningResolutionFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-screening-resolution: preview material or token changed.");
        }

        var committed = ResearchWorkspaceScreeningResolution.Commit(operationPreview);
        if (!committed.Completed)
        {
            return ScreeningResolutionFailure(MapScreeningResolutionStatus(committed.Status), committed.Message);
        }

        var overview = SafeBuild(preview.WorkspaceDirectory);
        return new DesktopScreeningResolutionCommandResult(
            DesktopWorkspaceCommandStatus.Succeeded,
            committed.Message,
            committed.DecisionId,
            committed.HeadDigest,
            committed.AlreadyApplied,
            overview);
    }

    public DesktopScreeningHandoffPreviewResult PreviewScreeningHandoff(DesktopScreeningHandoffRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = ResearchWorkspaceScreeningResolution.PreviewHandoff(new ResearchWorkspaceScreeningHandoffRequest(
            request.WorkspaceDirectory,
            request.ActorId,
            request.ActorKind,
            request.ActorRole,
            request.Rationale,
            request.OccurredAt));
        if (!result.IsReady)
        {
            return new DesktopScreeningHandoffPreviewResult(MapScreeningResolutionStatus(result.Status), result.Message, null);
        }

        var effects = result.ExpectedEffects;
        var preview = new DesktopScreeningHandoffPreview(
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
            result.JournalHeadDigest!,
            result.TargetSummaryDigests,
            result.ActorId!,
            result.ActorKind!,
            result.ActorRole!,
            result.Rationale!,
            result.OccurredAt,
            result.HandoffId!,
            result.HandoffDigest!,
            effects,
            ScreeningHandoffNonClaims,
            result.ConfirmationToken!,
            CreateScreeningHandoffConfirmationToken(result, effects, ScreeningHandoffNonClaims));
        return new DesktopScreeningHandoffPreviewResult(DesktopWorkspaceCommandStatus.Ready, result.Message, preview);
    }

    public DesktopScreeningHandoffCommandResult ExecuteScreeningHandoff(DesktopScreeningHandoffPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var operationPreview = OperationPreview(preview);
        string expectedToken;
        try
        {
            expectedToken = CreateScreeningHandoffConfirmationToken(preview);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            return ScreeningHandoffFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-screening-handoff: preview material is incomplete.");
        }
        if (!string.Equals(preview.ConfirmationToken, expectedToken, StringComparison.Ordinal))
        {
            return ScreeningHandoffFailure(
                DesktopWorkspaceCommandStatus.Stale,
                "stale-confirmation-screening-handoff: preview material or token changed.");
        }

        var committed = ResearchWorkspaceScreeningResolution.CommitHandoff(operationPreview);
        if (!committed.Completed)
        {
            return ScreeningHandoffFailure(MapScreeningResolutionStatus(committed.Status), committed.Message);
        }

        var overview = SafeBuild(preview.WorkspaceDirectory);
        return new DesktopScreeningHandoffCommandResult(
            DesktopWorkspaceCommandStatus.Succeeded,
            committed.Message,
            committed.HandoffId,
            committed.HandoffDigest,
            committed.AlreadyApplied,
            overview);
    }

    private static ResearchWorkspaceScreeningResolutionPreview OperationPreview(DesktopScreeningResolutionPreview value) => new(
        ResearchWorkspaceOperationStatus.Succeeded,
        ResearchWorkspaceExitCodes.Success,
        "Review the exact Screening resolution authority effects before confirmation.",
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
        value.TargetSummaryDigest,
        value.DecisionKind,
        value.Verdict,
        value.ActorId,
        value.ActorKind,
        value.ActorRole,
        value.Rationale,
        value.ExclusionReasonCode,
        value.SupersedesDecisionDigest,
        value.ResolvedConflictId,
        value.SourceDecisionDigests,
        value.OccurredAt,
        value.DecisionId,
        value.DecisionDigest,
        value.ExpectedEffects,
        value.OperationConfirmationToken);

    private static ResearchWorkspaceScreeningHandoffPreview OperationPreview(DesktopScreeningHandoffPreview value) => new(
        ResearchWorkspaceOperationStatus.Succeeded,
        ResearchWorkspaceExitCodes.Success,
        "Review the exact Screening handoff authority effects before confirmation.",
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
        value.JournalHeadDigest,
        value.TargetSummaryDigests,
        value.ActorId,
        value.ActorKind,
        value.ActorRole,
        value.Rationale,
        value.OccurredAt,
        value.HandoffId,
        value.HandoffDigest,
        value.ExpectedEffects,
        value.OperationConfirmationToken);

    internal static string CreateScreeningResolutionConfirmationToken(
        DesktopScreeningResolutionPreview value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var operation = OperationPreview(value);
        return ConfirmationToken(operation, operation.ExpectedEffects, value.NonClaims);
    }

    internal static string CreateScreeningResolutionConfirmationToken(
        ResearchWorkspaceScreeningResolutionPreview value,
        IReadOnlyList<string> effects,
        IReadOnlyList<string> nonClaims)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ConfirmationToken(value, effects, nonClaims);
    }

    internal static string CreateScreeningHandoffConfirmationToken(
        DesktopScreeningHandoffPreview value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var operation = OperationPreview(value);
        return ConfirmationToken(operation, operation.ExpectedEffects, value.NonClaims);
    }

    internal static string CreateScreeningHandoffConfirmationToken(
        ResearchWorkspaceScreeningHandoffPreview value,
        IReadOnlyList<string> effects,
        IReadOnlyList<string> nonClaims)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ConfirmationToken(value, effects, nonClaims);
    }

    private static string ConfirmationToken(
        ResearchWorkspaceScreeningResolutionPreview value,
        IReadOnlyList<string> effects,
        IReadOnlyList<string> nonClaims)
    {
        CanonicalJsonValue Optional(string? text) =>
            text is null ? CanonicalJsonValue.Null() : CanonicalJsonValue.From(text);
        var material = new CanonicalJsonObject()
            .Add("schema", "nexus.desktop.screening-resolution-preview")
            .Add("schema_version", "1.0.0")
            .Add("operation", "resolution")
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
            .Add("target_summary_digest", value.TargetSummaryDigest!)
            .Add("decision_kind", value.DecisionKind!)
            .Add("verdict", value.Verdict!)
            .Add("actor_id", value.ActorId!)
            .Add("actor_kind", value.ActorKind!)
            .Add("actor_role", value.ActorRole!)
            .Add("rationale", Optional(value.Rationale))
            .Add("exclusion_reason_code", Optional(value.ExclusionReasonCode))
            .Add("supersedes_decision_digest", Optional(value.SupersedesDecisionDigest))
            .Add("resolved_conflict_id", Optional(value.ResolvedConflictId))
            .Add("source_decision_digests", new CanonicalJsonArray(
                value.SourceDecisionDigests.Select(CanonicalJsonValue.From)))
            .Add("occurred_at",
                value.OccurredAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))
            .Add("decision_id", value.DecisionId!)
            .Add("decision_digest", value.DecisionDigest!)
            .Add("operation_confirmation_token", value.ConfirmationToken!)
            .Add("expected_effects", new CanonicalJsonArray(effects.Select(CanonicalJsonValue.From)))
            .Add("non_claims", new CanonicalJsonArray(nonClaims.Select(CanonicalJsonValue.From)));
        return ContentDigest.Sha256CanonicalJson(material).ToString();
    }

    private static string ConfirmationToken(
        ResearchWorkspaceScreeningHandoffPreview value,
        IReadOnlyList<string> effects,
        IReadOnlyList<string> nonClaims)
    {
        CanonicalJsonValue Optional(string? text) =>
            text is null ? CanonicalJsonValue.Null() : CanonicalJsonValue.From(text);
        var material = new CanonicalJsonObject()
            .Add("schema", "nexus.desktop.screening-handoff-preview")
            .Add("schema_version", "1.0.0")
            .Add("operation", "handoff")
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
            .Add("journal_head_digest", value.JournalHeadDigest!)
            .Add("target_summary_digests", new CanonicalJsonArray(
                value.TargetSummaryDigests.Select(CanonicalJsonValue.From)))
            .Add("actor_id", value.ActorId!)
            .Add("actor_kind", value.ActorKind!)
            .Add("actor_role", value.ActorRole!)
            .Add("rationale", Optional(value.Rationale))
            .Add("occurred_at",
                value.OccurredAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))
            .Add("handoff_id", value.HandoffId!)
            .Add("handoff_digest", value.HandoffDigest!)
            .Add("operation_confirmation_token", value.ConfirmationToken!)
            .Add("expected_effects", new CanonicalJsonArray(effects.Select(CanonicalJsonValue.From)))
            .Add("non_claims", new CanonicalJsonArray(nonClaims.Select(CanonicalJsonValue.From)));
        return ContentDigest.Sha256CanonicalJson(material).ToString();
    }

    private static DesktopWorkspaceCommandStatus MapScreeningResolutionStatus(ResearchWorkspaceOperationStatus status) =>
        status switch
        {
            ResearchWorkspaceOperationStatus.Succeeded => DesktopWorkspaceCommandStatus.Succeeded,
            ResearchWorkspaceOperationStatus.Stale => DesktopWorkspaceCommandStatus.Stale,
            ResearchWorkspaceOperationStatus.RecoveryRequired => DesktopWorkspaceCommandStatus.RecoveryRequired,
            _ => DesktopWorkspaceCommandStatus.Failed
        };

    private static DesktopScreeningResolutionCommandResult ScreeningResolutionFailure(
        DesktopWorkspaceCommandStatus status,
        string message) => new(status, message, null, null, false, null);

    private static DesktopScreeningHandoffCommandResult ScreeningHandoffFailure(
        DesktopWorkspaceCommandStatus status,
        string message) => new(status, message, null, null, false, null);
}
