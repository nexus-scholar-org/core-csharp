namespace NexusScholar.Desktop.AppServices;

public sealed record DesktopFullTextIntakeRequest(
    string WorkingDirectory,
    string CandidateId,
    string LocalPath,
    string ArtifactKind,
    string MediaType,
    string ActorId,
    string ActorKind,
    DateTimeOffset OccurredAt,
    long MaximumBytes,
    string? ExpectedSupersededManifestDigest = null);

public sealed record DesktopFullTextIntakePreview(
    string WorkspaceDirectory,
    string WorkspaceId,
    long ExpectedProjectRevision,
    string ScreeningAuthorityManifestDigest,
    string ScreeningConductManifestDigest,
    string ScreeningHandoffDigest,
    string CandidateId,
    string LocalPath,
    string ArtifactKind,
    string MediaType,
    string ActorId,
    string ActorKind,
    DateTimeOffset OccurredAt,
    long MaximumBytes,
    string? ExpectedSupersededManifestDigest,
    string AdmissionDigest,
    string InputDigest,
    string AcquisitionDigest,
    string ArtifactEvidenceDigest,
    string RawArtifactDigest,
    string ExtractionAttemptDigest,
    string? ExtractionStatus,
    string ResultingGenerationId,
    IReadOnlyList<string> ExpectedEffects,
    IReadOnlyList<string> NonClaims,
    string OperationConfirmationToken,
    string ConfirmationToken);

public sealed record DesktopFullTextIntakePreviewResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    DesktopFullTextIntakePreview? Preview)
{
    public bool IsReady => Status == DesktopWorkspaceCommandStatus.Ready && Preview is not null;
}

public sealed record DesktopFullTextIntakeCommandResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    string? CandidateId,
    string? GenerationId,
    string? RawArtifactDigest,
    bool AlreadyApplied,
    DesktopWorkspaceOverview? Overview)
{
    public bool Completed => Status is DesktopWorkspaceCommandStatus.Succeeded or DesktopWorkspaceCommandStatus.Attention;
}

public sealed record DesktopFullTextReviewRequest(
    string WorkingDirectory,
    string Verdict,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    string InclusionCriteria,
    string ExclusionCriteria,
    string ExclusionReasonCode,
    string? SelectedExclusionReasonCode,
    DateTimeOffset OccurredAt,
    string? CandidateId = null);

public sealed record DesktopFullTextReviewPreview(
    string WorkspaceDirectory,
    string WorkspaceId,
    long ExpectedProjectRevision,
    string ScreeningAuthorityManifestDigest,
    string ScreeningConductManifestDigest,
    string ScreeningHandoffDigest,
    string FullTextGenerationId,
    string FullTextManifestDigest,
    string CandidateId,
    string AdmissionDigest,
    string RawArtifactDigest,
    string ExtractionAttemptDigest,
    string? ExtractionStatus,
    string CriteriaDigest,
    string PolicyDigest,
    string HeaderDigest,
    string DecisionDigest,
    string ResultingHeadDigest,
    string Verdict,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    string InclusionCriteria,
    string ExclusionCriteria,
    string ExclusionReasonCode,
    string? SelectedExclusionReasonCode,
    DateTimeOffset OccurredAt,
    IReadOnlyList<string> ExpectedEffects,
    IReadOnlyList<string> NonClaims,
    string OperationConfirmationToken,
    string ConfirmationToken);

public sealed record DesktopFullTextReviewPreviewResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    DesktopFullTextReviewPreview? Preview)
{
    public bool IsReady => Status == DesktopWorkspaceCommandStatus.Ready && Preview is not null;
}

public sealed record DesktopFullTextReviewCommandResult(
    DesktopWorkspaceCommandStatus Status,
    string Message,
    string? CandidateId,
    string? DecisionDigest,
    string? HeadDigest,
    bool HandoffReady,
    bool AlreadyApplied,
    DesktopWorkspaceOverview? Overview)
{
    public bool Completed => Status is DesktopWorkspaceCommandStatus.Succeeded or DesktopWorkspaceCommandStatus.Attention;
}
