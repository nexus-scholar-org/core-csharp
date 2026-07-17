namespace NexusScholar.Desktop.AppServices;

public sealed record DesktopScreeningResolutionRequest(
    string WorkspaceDirectory,
    string CandidateId,
    string DecisionKind,
    string Verdict,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    string? ExclusionReasonCode,
    string? SupersedesDecisionDigest,
    string? ResolvedConflictId,
    IReadOnlyList<string> SourceDecisionDigests,
    DateTimeOffset OccurredAt);

public sealed record DesktopScreeningResolutionPreview(
    string WorkspaceDirectory,
    string WorkspaceId,
    long ExpectedProjectRevision,
    string AuthorityPackageGenerationId,
    string AuthorityPackageManifestDigest,
    string SourceResultDigest,
    string SourceSnapshotRecordDigest,
    string DecisionSetDigest,
    string ProtocolContentDigest,
    string CriteriaDigest,
    string CorpusBindingDigest,
    string ConductGenerationId,
    string ConductManifestDigest,
    string PolicyId,
    string PolicyDigest,
    string HeaderDigest,
    string PriorHeadDigest,
    string ResultingHeadDigest,
    int PriorEntryCount,
    string CandidateId,
    string TargetDigest,
    string TargetSummaryDigest,
    string DecisionKind,
    string Verdict,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    string? ExclusionReasonCode,
    string? SupersedesDecisionDigest,
    string? ResolvedConflictId,
    IReadOnlyList<string> SourceDecisionDigests,
    DateTimeOffset OccurredAt,
    string DecisionId,
    string DecisionDigest,
    IReadOnlyList<string> ExpectedEffects,
    IReadOnlyList<string> NonClaims,
    string OperationConfirmationToken,
    string ConfirmationToken);

public sealed record DesktopScreeningResolutionPreviewResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    DesktopScreeningResolutionPreview? Preview)
{
    public bool IsReady => Status == DesktopWorkspaceCommandStatus.Ready && Preview is not null;
}

public sealed record DesktopScreeningResolutionCommandResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    string? DecisionId,
    string? HeadDigest,
    bool AlreadyApplied,
    DesktopWorkspaceOverview? Overview)
{
    public bool Completed => Status is DesktopWorkspaceCommandStatus.Succeeded or DesktopWorkspaceCommandStatus.Attention;
}

public sealed record DesktopScreeningHandoffRequest(
    string WorkspaceDirectory,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    DateTimeOffset OccurredAt);

public sealed record DesktopScreeningHandoffPreview(
    string WorkspaceDirectory,
    string WorkspaceId,
    long ExpectedProjectRevision,
    string AuthorityPackageGenerationId,
    string AuthorityPackageManifestDigest,
    string SourceResultDigest,
    string SourceSnapshotRecordDigest,
    string DecisionSetDigest,
    string ProtocolContentDigest,
    string CriteriaDigest,
    string CorpusBindingDigest,
    string ConductGenerationId,
    string ConductManifestDigest,
    string PolicyId,
    string PolicyDigest,
    string HeaderDigest,
    string JournalHeadDigest,
    IReadOnlyList<string> TargetSummaryDigests,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    DateTimeOffset OccurredAt,
    string HandoffId,
    string HandoffDigest,
    IReadOnlyList<string> ExpectedEffects,
    IReadOnlyList<string> NonClaims,
    string OperationConfirmationToken,
    string ConfirmationToken);

public sealed record DesktopScreeningHandoffPreviewResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    DesktopScreeningHandoffPreview? Preview)
{
    public bool IsReady => Status == DesktopWorkspaceCommandStatus.Ready && Preview is not null;
}

public sealed record DesktopScreeningHandoffCommandResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    string? HandoffId,
    string? HandoffDigest,
    bool AlreadyApplied,
    DesktopWorkspaceOverview? Overview)
{
    public bool Completed => Status is DesktopWorkspaceCommandStatus.Succeeded or DesktopWorkspaceCommandStatus.Attention;
}
