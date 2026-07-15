namespace NexusScholar.ResearchWorkspace;

public sealed record ResearchWorkspaceGenerationManifest(
    string Schema,
    string GenerationId,
    string WorkspaceId,
    long ProjectRevision,
    IReadOnlyList<ResearchWorkspaceGenerationArtifact> Inputs,
    IReadOnlyList<ResearchWorkspaceGenerationArtifact> ImportTraces,
    IReadOnlyList<ResearchWorkspaceGenerationArtifact> Outputs)
{
    public const string CurrentSchema = "nexus.workspace-generation.v1";
}

public sealed record ResearchWorkspaceGenerationArtifact(string Name, string RelativePath, string Sha256);

public sealed record ResearchWorkspaceAnalysisCommit(
    ResearchWorkspaceAnalysisResult Analysis,
    ResearchWorkspaceProject Project,
    ResearchWorkspaceGenerationManifest Manifest);

public sealed record ResearchWorkspaceAuthorityGenerationManifest(
    string Schema,
    string AuthorityGenerationId,
    string WorkspaceId,
    long ProjectRevision,
    string SourceAnalysisGenerationId,
    string SourceAnalysisManifestSha256,
    string SourceResultId,
    string SourceResultDigest,
    string? PredecessorAuthorityGenerationId,
    string? PredecessorAuthorityGenerationManifestSha256,
    string AuthorityPolicyId,
    string AuthorityPolicyDigest,
    string DecisionSetDigest,
    IReadOnlyList<ResearchWorkspaceGenerationArtifact> Artifacts)
{
    public const string CurrentSchema = "nexus.workspace-authority-generation.v1";
}

public sealed record ResearchWorkspaceSuccessorAuthorityGenerationManifest(
    string Schema,
    string AuthorityGenerationId,
    string WorkspaceId,
    long ProjectRevision,
    string TransitionKind,
    string SourceAnalysisGenerationId,
    string SourceAnalysisManifestSha256,
    string SourceResultId,
    string SourceResultDigest,
    string PredecessorAuthorityGenerationId,
    string PredecessorAuthorityGenerationManifestSha256,
    string RequestId,
    string RequestDigest,
    string AuthorityPolicyId,
    string AuthorityPolicyDigest,
    string DecisionId,
    string DecisionDigest,
    string PredecessorSnapshotId,
    string PredecessorSnapshotRecordDigest,
    string SuccessorSnapshotId,
    string SuccessorSnapshotContentDigest,
    string SuccessorSnapshotRecordDigest,
    string InvalidationId,
    string InvalidationRecordDigest,
    string DecisionSetDigest,
    string DecisionRecordedEventDigest,
    string SnapshotInvalidatedEventDigest,
    string SnapshotPublicationEventDigest,
    IReadOnlyList<ResearchWorkspaceGenerationArtifact> Artifacts)
{
    public const string CurrentSchema = "nexus.workspace-authority-generation.v2";
    public const string DeduplicationDecisionTransition = "deduplication-decision";
}
