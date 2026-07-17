namespace NexusScholar.Desktop.AppServices;

public sealed record DesktopReportingWorkflowPreview(
    string WorkspaceDirectory,
    string WorkspaceId,
    long ExpectedProjectRevision,
    string ProtocolContentDigest,
    string ResultingGenerationId,
    string ResultingManifestDigest,
    IReadOnlyList<string> ExpectedEffects,
    string ConfirmationToken);

public sealed record DesktopReportingWorkflowPreviewResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    DesktopReportingWorkflowPreview? Preview);

public sealed record DesktopReportingWorkflowCommandResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    string? GenerationId,
    bool AlreadyApplied,
    DesktopWorkspaceOverview? Overview);

public sealed record DesktopReviewExportRequest(
    string WorkingDirectory,
    string ExportId,
    string ActorId,
    string ActorRole,
    DateTimeOffset OccurredAt,
    IReadOnlyList<string> Disclosures,
    IReadOnlyList<string> NonClaims);

public sealed record DesktopReviewFlowCounts(
    int Identified,
    int DuplicatesConsolidated,
    int PostDedup,
    int TitleAbstractIncluded,
    int TitleAbstractExcluded,
    int FullTextIncluded,
    int FullTextExcluded,
    int Included);

public sealed record DesktopReviewExportPreview(
    string WorkspaceDirectory,
    string WorkspaceId,
    long ExpectedProjectRevision,
    string ExportId,
    string ActorId,
    string ActorRole,
    DateTimeOffset OccurredAt,
    string ReportDigest,
    string SliceDigest,
    string WorkspaceCutDigest,
    string BundleManifestDigest,
    string InventoryDigest,
    string ExportRequestDigest,
    string? ExpectedPreviousLedgerEntryDigest,
    DesktopReviewFlowCounts Counts,
    IReadOnlyList<string> Disclosures,
    IReadOnlyList<string> NonClaims,
    IReadOnlyList<string> ExpectedEffects,
    string ConfirmationToken);

public sealed record DesktopReviewExportPreviewResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    DesktopReviewExportPreview? Preview);

public sealed record DesktopReviewExportCommandResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    string? ExportId,
    string? EntryDigest,
    long? Ordinal,
    bool AlreadyApplied,
    bool RoundTripVerified,
    DesktopWorkspaceOverview? Overview);
