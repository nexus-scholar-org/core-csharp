namespace NexusScholar.Desktop.AppServices;

public sealed record DesktopScreeningReviewDecisionSummary(
    string DecisionId,
    string DecisionDigest,
    string Kind,
    string Verdict,
    string ActorId,
    string ActorRole);

public sealed record DesktopScreeningReviewConflictSummary(
    string ConflictId,
    IReadOnlyList<string> SourceDecisionDigests,
    bool Resolved);

public sealed record DesktopScreeningReviewTarget(
    string CandidateId,
    string TargetDigest,
    string? CurrentVerdict,
    string? ExclusionReasonCode,
    IReadOnlyList<DesktopScreeningReviewDecisionSummary> CurrentDecisions,
    IReadOnlyList<DesktopScreeningReviewConflictSummary> Conflicts);

public sealed record DesktopScreeningReviewQueue(
    string WorkspaceId,
    long ProjectRevision,
    string AuthorityPackageGenerationId,
    string AuthorityPackageManifestDigest,
    string ConductGenerationId,
    string ConductManifestDigest,
    string PolicyId,
    string PolicyDigest,
    string CriteriaId,
    string CriteriaDigest,
    int RequiredReviewCount,
    IReadOnlyList<string> AssignedActorRoles,
    IReadOnlyList<string> AdjudicatorRoles,
    IReadOnlyList<string> ExclusionReasons,
    bool HandoffReady,
    IReadOnlyList<DesktopScreeningReviewTarget> Targets);

public sealed record DesktopScreeningReviewQueueResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    DesktopScreeningReviewQueue? Queue)
{
    public bool Completed => Status is DesktopWorkspaceCommandStatus.Succeeded or DesktopWorkspaceCommandStatus.Attention;
}

public sealed record DesktopScreeningReviewRequest(
    string WorkspaceDirectory,
    string CandidateId,
    string DecisionKind,
    string Verdict,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    string? ExclusionReasonCode,
    DateTimeOffset OccurredAt);

public sealed record DesktopScreeningReviewPreview(
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
    string DecisionKind,
    string Verdict,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    string? ExclusionReasonCode,
    IReadOnlyList<string> SourceDecisionDigests,
    DateTimeOffset OccurredAt,
    string DecisionId,
    string DecisionDigest,
    IReadOnlyList<string> ExpectedEffects,
    IReadOnlyList<string> NonClaims,
    string ConfirmationToken);

public sealed record DesktopScreeningReviewPreviewResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    DesktopScreeningReviewPreview? Preview)
{
    public bool IsReady => Status == DesktopWorkspaceCommandStatus.Ready && Preview is not null;
}

public sealed record DesktopScreeningReviewCommandResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    string? DecisionId,
    string? HeadDigest,
    bool AlreadyApplied,
    DesktopWorkspaceOverview? Overview,
    DesktopScreeningReviewQueue? Queue)
{
    public bool Completed => Status is DesktopWorkspaceCommandStatus.Succeeded or DesktopWorkspaceCommandStatus.Attention;
}
