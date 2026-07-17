using NexusScholar.Deduplication;
using NexusScholar.Kernel;
using NexusScholar.Screening;
using NexusScholar.Screening.CorpusSnapshots;

namespace NexusScholar.ResearchWorkspace;

public sealed record ResearchWorkspaceScreeningDecisionSummary(
    string DecisionId,
    string DecisionDigest,
    string Kind,
    string Verdict,
    string ActorId,
    string ActorRole);

public sealed record ResearchWorkspaceScreeningConflictSummary(
    string ConflictId,
    IReadOnlyList<string> SourceDecisionDigests,
    bool Resolved);

public sealed record ResearchWorkspaceScreeningReviewTarget(
    string CandidateId,
    string TargetDigest,
    string? CurrentVerdict,
    string? ExclusionReasonCode,
    IReadOnlyList<ResearchWorkspaceScreeningDecisionSummary> CurrentDecisions,
    IReadOnlyList<ResearchWorkspaceScreeningConflictSummary> Conflicts);

public sealed record ResearchWorkspaceScreeningReviewQueue(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    string? WorkspaceId,
    long? ProjectRevision,
    string? AuthorityPackageGenerationId,
    string? AuthorityPackageManifestDigest,
    string? ConductGenerationId,
    string? ConductManifestDigest,
    string? PolicyId,
    string? PolicyDigest,
    string? CriteriaId,
    string? CriteriaDigest,
    int RequiredReviewCount,
    IReadOnlyList<string> AssignedActorRoles,
    IReadOnlyList<string> AdjudicatorRoles,
    IReadOnlyList<string> ExclusionReasons,
    bool HandoffReady,
    IReadOnlyList<ResearchWorkspaceScreeningReviewTarget> Targets)
{
    public bool Completed => Status == ResearchWorkspaceOperationStatus.Succeeded;
}

public sealed record ResearchWorkspaceScreeningReviewRequest(
    string WorkingDirectory,
    string CandidateId,
    string DecisionKind,
    string Verdict,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    string? ExclusionReasonCode,
    DateTimeOffset OccurredAt);

public sealed record ResearchWorkspaceScreeningReviewPreview(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    string WorkspaceDirectory,
    string? WorkspaceId,
    long? ExpectedProjectRevision,
    string? AuthorityPackageGenerationId,
    string? AuthorityPackageManifestDigest,
    string? SourceResultDigest,
    string? SourceSnapshotRecordDigest,
    string? DecisionSetDigest,
    string? ProtocolContentDigest,
    string? CriteriaDigest,
    string? CorpusBindingDigest,
    string? ConductGenerationId,
    string? ConductManifestDigest,
    string? PolicyId,
    string? PolicyDigest,
    string? HeaderDigest,
    string? PriorHeadDigest,
    string? ResultingHeadDigest,
    int PriorEntryCount,
    string? CandidateId,
    string? TargetDigest,
    string? DecisionKind,
    string? Verdict,
    string? ActorId,
    string? ActorKind,
    string? ActorRole,
    string? Rationale,
    string? ExclusionReasonCode,
    IReadOnlyList<string> SourceDecisionDigests,
    DateTimeOffset OccurredAt,
    string? DecisionId,
    string? DecisionDigest,
    IReadOnlyList<string> ExpectedEffects,
    string? ConfirmationToken)
{
    public bool IsReady => Status == ResearchWorkspaceOperationStatus.Succeeded &&
        ConfirmationToken is not null && DecisionDigest is not null;
}

public sealed record ResearchWorkspaceScreeningReviewCommitResult(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    ResearchWorkspaceProject? Project,
    string? DecisionId,
    string? HeadDigest,
    bool AlreadyApplied)
{
    public bool Completed => Status == ResearchWorkspaceOperationStatus.Succeeded;
}

public static class ResearchWorkspaceScreeningReview
{
    private static readonly string[] DecisionEffects =
    [
        "append one human Screening conduct decision",
        "publish one immutable Screening conduct successor generation",
        "advance the workspace project revision"
    ];

    public static ResearchWorkspaceScreeningReviewQueue Inspect(string workingDirectory)
    {
        try
        {
            var state = Load(workingDirectory);
            var current = CurrentDecisionDigests(state.Conduct.Journal);
            var targets = state.Conduct.Header.CandidateIds.Select(candidateId =>
            {
                state.Conduct.Journal.Projection.Outcomes.TryGetValue(candidateId, out var outcome);
                var decisions = state.Conduct.Journal.Decisions
                    .Where(item => item.CandidateId == candidateId && current.Contains(item.Digest))
                    .OrderBy(item => item.Ordinal)
                    .Select(item => new ResearchWorkspaceScreeningDecisionSummary(
                        item.DecisionId, item.Digest.ToString(), Kind(item.Kind), item.Verdict,
                        item.Actor.ActorId, item.Actor.Role))
                    .ToArray();
                var conflicts = state.Conduct.Journal.Projection.Conflicts
                    .Where(item => item.CandidateId == candidateId)
                    .Select(item => new ResearchWorkspaceScreeningConflictSummary(
                        item.ConflictId,
                        item.SourceDecisionDigests.Select(value => value.ToString()).ToArray(),
                        item.Resolved))
                    .ToArray();
                return new ResearchWorkspaceScreeningReviewTarget(
                    candidateId, TargetDigest(state, candidateId).ToString(),
                    outcome?.Verdict, outcome?.ExclusionReasonCode, decisions, conflicts);
            }).ToArray();
            var policy = state.Conduct.Policy;
            return new ResearchWorkspaceScreeningReviewQueue(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                policy.CandidateSet.Candidates.Count == 0
                    ? "No title/abstract Screening targets are available."
                    : $"{targets.Length} title/abstract Screening target(s) loaded.",
                state.Project.WorkspaceId,
                state.Project.Revision,
                state.Package.Manifest.GenerationId,
                state.Project.ScreeningAuthorityPackageManifestSha256,
                state.Conduct.Manifest.GenerationId,
                state.Project.ScreeningConductManifestSha256,
                policy.PolicyId,
                policy.Digest.ToString(),
                policy.Criteria.CriteriaId,
                policy.CriteriaDigest.ToString(),
                policy.RequiredReviewCount,
                policy.Assignments.Select(item => $"{item.ActorId}|{item.Role}").ToArray(),
                policy.AdjudicatorRoles.ToArray(),
                policy.ExclusionReasons.Select(item => item.Code).ToArray(),
                state.Conduct.Journal.Projection.HandoffReady,
                targets);
        }
        catch (Exception exception)
        {
            var classified = Classify(exception);
            return new ResearchWorkspaceScreeningReviewQueue(
                classified.Status, classified.ExitCode, classified.Message,
                null, null, null, null, null, null, null, null, null, null, 0,
                [], [], [], false, []);
        }
    }

    public static ResearchWorkspaceScreeningReviewPreview Preview(
        ResearchWorkspaceScreeningReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return Prepare(request).Preview;
        }
        catch (Exception exception)
        {
            var classified = Classify(exception);
            return FailedPreview(request, classified);
        }
    }

    public static ResearchWorkspaceScreeningReviewCommitResult Commit(
        ResearchWorkspaceScreeningReviewPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.IsReady)
            return FailedCommit(ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "An exact successful Screening review preview is required.");
        try
        {
            var request = new ResearchWorkspaceScreeningReviewRequest(
                preview.WorkspaceDirectory, preview.CandidateId!, preview.DecisionKind!,
                preview.Verdict!, preview.ActorId!, preview.ActorKind!, preview.ActorRole!, preview.Rationale!,
                preview.ExclusionReasonCode, preview.OccurredAt);
            var prepared = Prepare(request);
            if (!Same(preview, prepared.Preview))
                return FailedCommit(ResearchWorkspaceOperationStatus.Stale,
                    ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                    "stale-screening-review-preview: authority or decision material changed.");
            var entries = prepared.State.Conduct.Entries.Append<IScreeningConductEntry>(prepared.Decision).ToArray();
            var commit = ResearchWorkspaceScreeningConductTransaction.Commit(
                prepared.State.Location,
                prepared.State.Project,
                prepared.State.Package.Deduplication,
                prepared.State.Package.Protocol,
                prepared.State.Package.Criteria,
                prepared.State.Conduct.Policy,
                prepared.State.Conduct.Header,
                entries,
                corpusBinding: prepared.State.Conduct.CorpusBinding,
                sourceAuthority: prepared.State.Package.SourceResultAuthority,
                corpusSnapshot: prepared.State.Package.DeduplicationAuthorityChain.CurrentSnapshot);
            return new ResearchWorkspaceScreeningReviewCommitResult(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                commit.AlreadyApplied ? "Screening decision was already applied." : "Screening decision committed.",
                commit.Project,
                prepared.Decision.DecisionId,
                commit.Journal.Projection.HeadDigest.ToString(),
                commit.AlreadyApplied);
        }
        catch (Exception exception) when (exception is ArgumentException or ScreeningRuleException)
        {
            return FailedCommit(
                ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "stale-screening-review-preview: decision material no longer reproduces.");
        }
        catch (Exception exception)
        {
            var classified = Classify(exception);
            return FailedCommit(classified.Status, classified.ExitCode, classified.Message);
        }
    }

    private static Prepared Prepare(ResearchWorkspaceScreeningReviewRequest request)
    {
        var state = Load(request.WorkingDirectory);
        var candidateId = Required(request.CandidateId, nameof(request.CandidateId));
        if (!state.Conduct.Header.CandidateIds.Contains(candidateId, StringComparer.Ordinal))
            throw new ArgumentException("Candidate must identify an exact current Screening target.");
        if (state.Conduct.Journal.Projection.Outcomes.ContainsKey(candidateId))
            throw new ArgumentException(
                "The Screening target is closed; a later slice must provide exact supersession authority.");
        var actor = new ScreeningConductActor(
            Required(request.ActorId, nameof(request.ActorId)),
            Required(request.ActorKind, nameof(request.ActorKind)),
            Required(request.ActorRole, nameof(request.ActorRole)));
        var kind = ParseReviewKind(request.DecisionKind);
        var targetDigest = TargetDigest(state, candidateId);
        var sourceDigests = Array.Empty<ContentDigest>();
        var previous = state.Conduct.Journal.Projection.HeadDigest;
        var requestId = "screening-request-" + ContentDigest.Sha256Utf8(
            $"{state.Project.WorkspaceId}|{state.Project.Revision}|{candidateId}|{Kind(kind)}|" +
            $"{targetDigest}|{request.Verdict}|{actor.ActorId}|{actor.Kind}|{actor.Role}|{request.OccurredAt:O}").Value[7..23];
        var decision = ScreeningConductDecision.Create(
            state.Conduct.Header, state.Conduct.Entries.Count + 1, previous, requestId,
            candidateId, kind, Required(request.Verdict, nameof(request.Verdict)), actor,
            Required(request.Rationale, nameof(request.Rationale)), request.OccurredAt,
            Optional(request.ExclusionReasonCode), null, null, sourceDigests);
        var journal = ScreeningConductJournal.RehydrateEntries(
            state.Conduct.Header, state.Conduct.Policy,
            state.Conduct.Entries.Append<IScreeningConductEntry>(decision));
        var token = Token(
            state.Project.WorkspaceId, state.Project.Revision, state.Package.Manifest.GenerationId,
            state.Project.ScreeningAuthorityPackageManifestSha256!,
            state.Package.Manifest.SourceResultDigest,
            state.Package.Manifest.SourceSnapshotRecordDigest,
            state.Package.Manifest.DecisionSetDigest,
            state.Package.Manifest.ProtocolContentDigest,
            state.Package.Manifest.CriteriaDigest,
            state.Conduct.CorpusBinding!.BindingDigest.ToString(),
            state.Conduct.Manifest.GenerationId, state.Project.ScreeningConductManifestSha256!,
            candidateId, targetDigest.ToString(), actor.Kind,
            previous.ToString(), decision.Digest.ToString(), request.OccurredAt, DecisionEffects);
        var preview = new ResearchWorkspaceScreeningReviewPreview(
            ResearchWorkspaceOperationStatus.Succeeded, ResearchWorkspaceExitCodes.Success,
            "Review the exact Screening authority effects before confirmation.",
            state.Location.RootDirectory, state.Project.WorkspaceId, state.Project.Revision,
            state.Package.Manifest.GenerationId, state.Project.ScreeningAuthorityPackageManifestSha256,
            state.Package.Manifest.SourceResultDigest,
            state.Package.Manifest.SourceSnapshotRecordDigest,
            state.Package.Manifest.DecisionSetDigest,
            state.Package.Manifest.ProtocolContentDigest,
            state.Package.Manifest.CriteriaDigest,
            state.Conduct.CorpusBinding!.BindingDigest.ToString(),
            state.Conduct.Manifest.GenerationId, state.Project.ScreeningConductManifestSha256,
            state.Conduct.Policy.PolicyId, state.Conduct.Policy.Digest.ToString(),
            state.Conduct.Header.Digest.ToString(), previous.ToString(),
            journal.Projection.HeadDigest.ToString(), state.Conduct.Entries.Count,
            candidateId, targetDigest.ToString(), Kind(kind), decision.Verdict,
            actor.ActorId, actor.Kind, actor.Role,
            decision.Rationale, decision.ExclusionReasonCode,
            sourceDigests.Select(item => item.ToString()).ToArray(),
            request.OccurredAt, decision.DecisionId, decision.Digest.ToString(),
            DecisionEffects, token);
        return new Prepared(state, decision, preview);
    }

    private static State Load(string workingDirectory)
    {
        var package = ResearchWorkspaceScreeningAuthorityPackage.VerifyCurrent(workingDirectory);
        var location = ResearchWorkspaceStore.FindFrom(Path.GetFullPath(Required(
            workingDirectory, nameof(workingDirectory))))
            ?? throw new ResearchWorkspaceMissingInputException("No Nexus research workspace was found.");
        var project = ResearchWorkspaceStore.ReadProject(location.ProjectFilePath);
        var conduct = ResearchWorkspaceScreeningConductVerifier.VerifyCurrent(
            location, project, package.Deduplication, package.Protocol, package.Criteria,
            package.SourceResultAuthority, package.DeduplicationAuthorityChain.CurrentSnapshot);
        if (conduct.CorpusBinding is null)
            throw new InvalidOperationException("Screening conduct lacks a verified corpus binding.");
        _ = ScreeningCorpusBindingAuthority.VerifyConductPolicyBinding(
            conduct.CorpusBinding, conduct.Policy);
        return new State(location, project, package, conduct);
    }

    private static HashSet<ContentDigest> CurrentDecisionDigests(ScreeningConductJournal journal)
    {
        var superseded = journal.Decisions
            .Where(item => item.Kind == ScreeningConductDecisionKind.Correction &&
                item.SupersedesDecisionDigest is not null)
            .Select(item => item.SupersedesDecisionDigest!.Value);
        return journal.Decisions.Select(item => item.Digest)
            .Except(superseded).Except(journal.Projection.InvalidatedDecisionDigests).ToHashSet();
    }

    private static string Token(
        string workspaceId,
        long revision,
        string authorityGeneration,
        string authorityManifestDigest,
        string sourceResultDigest,
        string sourceSnapshotRecordDigest,
        string decisionSetDigest,
        string protocolContentDigest,
        string criteriaDigest,
        string corpusBindingDigest,
        string conductGeneration,
        string conductManifestDigest,
        string candidateId,
        string targetDigest,
        string actorKind,
        string priorHead,
        string resultDigest,
        DateTimeOffset occurredAt,
        IReadOnlyList<string> effects) =>
        ContentDigest.Sha256CanonicalJson(new CanonicalJsonObject()
            .Add("schema", "nexus.workspace-screening-preview")
            .Add("schema_version", "1.0.0")
            .Add("workspace_id", workspaceId).Add("project_revision", revision)
            .Add("authority_generation", authorityGeneration)
            .Add("authority_manifest_digest", authorityManifestDigest)
            .Add("source_result_digest", sourceResultDigest)
            .Add("source_snapshot_record_digest", sourceSnapshotRecordDigest)
            .Add("decision_set_digest", decisionSetDigest)
            .Add("protocol_content_digest", protocolContentDigest)
            .Add("criteria_digest", criteriaDigest)
            .Add("corpus_binding_digest", corpusBindingDigest)
            .Add("conduct_generation", conductGeneration)
            .Add("conduct_manifest_digest", conductManifestDigest)
            .Add("candidate_id", candidateId)
            .Add("target_digest", targetDigest)
            .Add("actor_kind", actorKind)
            .Add("prior_head_digest", priorHead).Add("result_digest", resultDigest)
            .AddTimestamp("occurred_at", occurredAt)
            .Add("effects", CanonicalJsonValue.Array(effects.Select(CanonicalJsonValue.From).ToArray())))
        .ToString();

    private static bool Same(
        ResearchWorkspaceScreeningReviewPreview left,
        ResearchWorkspaceScreeningReviewPreview right) =>
        left == right ||
        left with
        {
            SourceDecisionDigests = right.SourceDecisionDigests,
            ExpectedEffects = right.ExpectedEffects
        } == right &&
        left.SourceDecisionDigests.SequenceEqual(right.SourceDecisionDigests, StringComparer.Ordinal) &&
        left.ExpectedEffects.SequenceEqual(right.ExpectedEffects, StringComparer.Ordinal);

    private static ScreeningConductDecisionKind ParseReviewKind(string value) =>
        Required(value, nameof(value)).ToLowerInvariant() switch
        {
            "review" => ScreeningConductDecisionKind.Review,
            _ => throw new ArgumentException("Slice 5 admits title/abstract review decisions only.")
        };

    private static string Kind(ScreeningConductDecisionKind value) =>
        value.ToString().ToLowerInvariant();

    private static ContentDigest TargetDigest(State state, string candidateId)
    {
        var candidate = state.Conduct.Policy.CandidateSet.Candidates.Single(item =>
            string.Equals(item.CandidateId, candidateId, StringComparison.Ordinal));
        var candidateDigest = DeduplicationAuthorityDigests
            .CreateCandidateDigestMaterial(candidate).CandidateDigest;
        return ContentDigest.Sha256CanonicalJson(new CanonicalJsonObject()
            .Add("schema", "nexus.workspace-screening-target")
            .Add("schema_version", "1.0.0")
            .Add("candidate_id", candidateId)
            .Add("candidate_digest", candidateDigest.ToString())
            .Add("corpus_binding_digest", state.Conduct.CorpusBinding!.BindingDigest.ToString())
            .Add("policy_digest", state.Conduct.Policy.Digest.ToString()));
    }

    private static ResearchWorkspaceScreeningReviewPreview FailedPreview(
        ResearchWorkspaceScreeningReviewRequest request,
        Classification failure) => new(
        failure.Status, failure.ExitCode, failure.Message, Path.GetFullPath(request.WorkingDirectory),
        null, null, null, null, null, null, null, null, null, null, null, null,
        null, null, null, null, null, 0,
        request.CandidateId, null, request.DecisionKind, request.Verdict, request.ActorId,
        request.ActorKind, request.ActorRole, request.Rationale, request.ExclusionReasonCode,
        [], request.OccurredAt, null, null, [], null);

    private static ResearchWorkspaceScreeningReviewCommitResult FailedCommit(
        ResearchWorkspaceOperationStatus status,
        int exitCode,
        string message) => new(status, exitCode, message, null, null, null, false);

    private static Classification Classify(Exception exception) => exception switch
    {
        ResearchWorkspaceScreeningAuthorityException authority
            when authority.Category == ResearchWorkspaceScreeningAuthorityPackage.StaleCategory =>
            new(ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure, authority.Message),
        ResearchWorkspaceConcurrencyException concurrency when concurrency.InnerException is not IOException =>
            new(ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure, concurrency.Message),
        IOException or UnauthorizedAccessException =>
            new(ResearchWorkspaceOperationStatus.RecoveryRequired,
                ResearchWorkspaceExitCodes.UnexpectedRuntimeFailure,
                "Screening review could not access the local workspace safely."),
        ResearchWorkspaceMissingInputException =>
            new(ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.MissingProjectOrInput, exception.Message),
        ArgumentException or ScreeningRuleException =>
            new(ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure, exception.Message),
        _ => new(ResearchWorkspaceOperationStatus.RecoveryRequired,
            ResearchWorkspaceExitCodes.UnexpectedRuntimeFailure,
            "Screening review authority could not be reconstructed from the local workspace.")
    };

    private static string Required(string? value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        return value.Trim();
    }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record State(
        ResearchWorkspaceLocation Location,
        ResearchWorkspaceProject Project,
        VerifiedResearchWorkspaceScreeningAuthorityPackage Package,
        VerifiedResearchWorkspaceScreeningConduct Conduct);

    private sealed record Prepared(
        State State,
        ScreeningConductDecision Decision,
        ResearchWorkspaceScreeningReviewPreview Preview);

    private sealed record Classification(
        ResearchWorkspaceOperationStatus Status,
        int ExitCode,
        string Message);
}
