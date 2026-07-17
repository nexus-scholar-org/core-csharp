using NexusScholar.ResearchWorkspace;

namespace NexusScholar.Desktop.AppServices;

public sealed partial class DesktopWorkspaceCommandFacade
{
    public DesktopReportingWorkflowPreviewResult PreviewReportingWorkflow(
        string workingDirectory)
    {
        var result = ResearchWorkspaceReportingWorkflow.Preview(workingDirectory);
        if (!result.IsReady)
            return new(MapReviewStatus(result.Status), result.Message, null);
        return new(DesktopWorkspaceCommandStatus.Ready, result.Message,
            new DesktopReportingWorkflowPreview(
                result.WorkspaceDirectory, result.WorkspaceId!,
                result.ExpectedProjectRevision!.Value, result.ProtocolContentDigest!,
                result.ResultingGenerationId!, result.ResultingManifestDigest!,
                result.ExpectedEffects, result.ConfirmationToken!));
    }

    public DesktopReportingWorkflowCommandResult ExecuteReportingWorkflow(
        DesktopReportingWorkflowPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var result = ResearchWorkspaceReportingWorkflow.Commit(
            new ResearchWorkspaceReportingWorkflowPreview(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                "Review the reporting Workflow authority effects before confirmation.",
                preview.WorkspaceDirectory, preview.WorkspaceId,
                preview.ExpectedProjectRevision, preview.ProtocolContentDigest,
                preview.ResultingGenerationId, preview.ResultingManifestDigest,
                preview.ExpectedEffects, preview.ConfirmationToken));
        return new DesktopReportingWorkflowCommandResult(
            MapReviewStatus(result.Status), result.Message, result.GenerationId,
            result.AlreadyApplied,
            result.Completed ? SafeBuild(preview.WorkspaceDirectory) : null);
    }

    public DesktopReviewExportPreviewResult PreviewReviewExport(
        DesktopReviewExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = ResearchWorkspaceReviewExportWorkflow.Preview(
            new ResearchWorkspaceReviewExportRequest(
                request.WorkingDirectory, request.ExportId, request.ActorId,
                request.ActorRole, request.OccurredAt, request.Disclosures, request.NonClaims));
        if (!result.IsReady)
            return new(MapReviewStatus(result.Status), result.Message, null);
        return new(DesktopWorkspaceCommandStatus.Ready, result.Message,
            new DesktopReviewExportPreview(
                result.WorkspaceDirectory, result.WorkspaceId!,
                result.ExpectedProjectRevision!.Value, result.ExportId!, result.ActorId!,
                result.ActorRole!, result.OccurredAt, result.ReportDigest!, result.SliceDigest!,
                result.WorkspaceCutDigest!, result.BundleManifestDigest!,
                result.InventoryDigest!, result.ExportRequestDigest!,
                result.ExpectedPreviousLedgerEntryDigest, new DesktopReviewFlowCounts(
                    result.Counts!.Identified,
                    result.Counts.DuplicatesConsolidated,
                    result.Counts.PostDedup,
                    result.Counts.TitleAbstractIncluded,
                    result.Counts.TitleAbstractExcluded,
                    result.Counts.FullTextIncluded,
                    result.Counts.FullTextExcluded,
                    result.Counts.Included),
                result.Disclosures, result.NonClaims, result.ExpectedEffects,
                result.ConfirmationToken!));
    }

    public DesktopReviewExportCommandResult ExecuteReviewExport(
        DesktopReviewExportPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var result = ResearchWorkspaceReviewExportWorkflow.Commit(
            new ResearchWorkspaceReviewExportPreview(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                "Review the exact report, Bundle v2, and export publication effects before confirmation.",
                preview.WorkspaceDirectory, preview.WorkspaceId,
                preview.ExpectedProjectRevision, preview.ExportId, preview.ActorId,
                preview.ActorRole, preview.OccurredAt, preview.ReportDigest, preview.SliceDigest,
                preview.WorkspaceCutDigest, preview.BundleManifestDigest,
                preview.InventoryDigest, preview.ExportRequestDigest,
                preview.ExpectedPreviousLedgerEntryDigest,
                new ResearchWorkspaceReviewFlowCounts(
                    preview.Counts.Identified,
                    preview.Counts.DuplicatesConsolidated,
                    preview.Counts.PostDedup,
                    preview.Counts.TitleAbstractIncluded,
                    preview.Counts.TitleAbstractExcluded,
                    preview.Counts.FullTextIncluded,
                    preview.Counts.FullTextExcluded,
                    preview.Counts.Included),
                preview.Disclosures, preview.NonClaims, preview.ExpectedEffects,
                preview.ConfirmationToken));
        return new DesktopReviewExportCommandResult(
            MapReviewStatus(result.Status), result.Message, result.ExportId,
            result.EntryDigest, result.Ordinal, result.AlreadyApplied,
            result.RoundTripVerified,
            result.Completed ? SafeBuild(preview.WorkspaceDirectory) : null);
    }

    private static DesktopWorkspaceCommandStatus MapReviewStatus(
        ResearchWorkspaceOperationStatus status) => status switch
        {
            ResearchWorkspaceOperationStatus.Succeeded =>
                DesktopWorkspaceCommandStatus.Succeeded,
            ResearchWorkspaceOperationStatus.Stale =>
                DesktopWorkspaceCommandStatus.Stale,
            ResearchWorkspaceOperationStatus.RecoveryRequired =>
                DesktopWorkspaceCommandStatus.RecoveryRequired,
            _ => DesktopWorkspaceCommandStatus.Failed
        };
}
