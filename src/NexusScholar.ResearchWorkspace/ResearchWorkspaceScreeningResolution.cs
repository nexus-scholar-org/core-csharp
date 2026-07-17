using NexusScholar.Deduplication;
using NexusScholar.Kernel;
using NexusScholar.Screening;
using NexusScholar.Screening.CorpusSnapshots;

namespace NexusScholar.ResearchWorkspace;

public sealed record ResearchWorkspaceScreeningResolutionRequest(
    string WorkingDirectory,
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

public sealed record ResearchWorkspaceScreeningResolutionPreview(
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
    string? TargetSummaryDigest,
    string? DecisionKind,
    string? Verdict,
    string? ActorId,
    string? ActorKind,
    string? ActorRole,
    string? Rationale,
    string? ExclusionReasonCode,
    string? SupersedesDecisionDigest,
    string? ResolvedConflictId,
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

public sealed record ResearchWorkspaceScreeningResolutionCommitResult(
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

public sealed record ResearchWorkspaceScreeningHandoffRequest(
    string WorkingDirectory,
    string ActorId,
    string ActorKind,
    string ActorRole,
    string Rationale,
    DateTimeOffset OccurredAt);

public sealed record ResearchWorkspaceScreeningHandoffPreview(
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
    string? JournalHeadDigest,
    IReadOnlyList<string> TargetSummaryDigests,
    string? ActorId,
    string? ActorKind,
    string? ActorRole,
    string? Rationale,
    DateTimeOffset OccurredAt,
    string? HandoffId,
    string? HandoffDigest,
    IReadOnlyList<string> ExpectedEffects,
    string? ConfirmationToken)
{
    public bool IsReady => Status == ResearchWorkspaceOperationStatus.Succeeded &&
        ConfirmationToken is not null && HandoffDigest is not null;
}

public sealed record ResearchWorkspaceScreeningHandoffCommitResult(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    ResearchWorkspaceProject? Project,
    string? HandoffId,
    string? HandoffDigest,
    bool AlreadyApplied)
{
    public bool Completed => Status == ResearchWorkspaceOperationStatus.Succeeded;
}

public static class ResearchWorkspaceScreeningResolution
{
    private static readonly string[] CorrectionEffects =
    [
        "append one human Screening correction",
        "supersede one exact current reviewer decision",
        "publish one immutable Screening conduct successor generation",
        "advance the workspace project revision"
    ];

    private static readonly string[] AdjudicationEffects =
    [
        "append one human Screening adjudication",
        "resolve one exact current conflict and its source decisions",
        "publish one immutable Screening conduct successor generation",
        "advance the workspace project revision"
    ];

    private static readonly string[] HandoffEffects =
    [
        "publish one immutable Screening handoff over the exact current journal",
        "preserve every terminal target outcome and supporting decision digest",
        "advance the workspace project revision"
    ];

    public static ResearchWorkspaceScreeningResolutionPreview Preview(
        ResearchWorkspaceScreeningResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return PrepareResolution(request).Preview;
        }
        catch (Exception exception)
        {
            return FailedResolutionPreview(request, Classify(exception));
        }
    }

    public static ResearchWorkspaceScreeningResolutionCommitResult Commit(
        ResearchWorkspaceScreeningResolutionPreview preview,
        Action<ResearchWorkspaceAuthorityFaultPoint>? faultInjector = null)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.IsReady)
            return FailedResolutionCommit(
                ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "An exact successful Screening resolution preview is required.");
        try
        {
            var request = new ResearchWorkspaceScreeningResolutionRequest(
                preview.WorkspaceDirectory, preview.CandidateId!, preview.DecisionKind!,
                preview.Verdict!, preview.ActorId!, preview.ActorKind!, preview.ActorRole!,
                preview.Rationale!, preview.ExclusionReasonCode, preview.SupersedesDecisionDigest,
                preview.ResolvedConflictId, preview.SourceDecisionDigests, preview.OccurredAt);
            var prepared = PrepareResolution(request);
            if (!Same(preview, prepared.Preview))
                return FailedResolutionCommit(
                    ResearchWorkspaceOperationStatus.Stale,
                    ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                    "stale-screening-resolution-preview: authority, target summary, actor, or source decisions changed.");
            var commit = Commit(
                prepared.State,
                prepared.State.Conduct.Entries.Append<IScreeningConductEntry>(prepared.Decision).ToArray(),
                handoff: null,
                faultInjector);
            return new ResearchWorkspaceScreeningResolutionCommitResult(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                commit.AlreadyApplied ? "Screening resolution was already applied." : "Screening resolution committed.",
                commit.Project,
                prepared.Decision.DecisionId,
                commit.Journal.Projection.HeadDigest.ToString(),
                commit.AlreadyApplied);
        }
        catch (Exception exception) when (exception is ArgumentException or ScreeningRuleException)
        {
            return FailedResolutionCommit(
                ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "stale-screening-resolution-preview: decision material no longer reproduces.");
        }
        catch (Exception exception)
        {
            var classified = Classify(exception);
            return FailedResolutionCommit(classified.Status, classified.ExitCode, classified.Message);
        }
    }

    public static ResearchWorkspaceScreeningHandoffPreview PreviewHandoff(
        ResearchWorkspaceScreeningHandoffRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return PrepareHandoff(request).Preview;
        }
        catch (Exception exception)
        {
            return FailedHandoffPreview(request, Classify(exception));
        }
    }

    public static ResearchWorkspaceScreeningHandoffCommitResult CommitHandoff(
        ResearchWorkspaceScreeningHandoffPreview preview,
        Action<ResearchWorkspaceAuthorityFaultPoint>? faultInjector = null)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.IsReady)
            return FailedHandoffCommit(
                ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "An exact successful Screening handoff preview is required.");
        try
        {
            var request = new ResearchWorkspaceScreeningHandoffRequest(
                preview.WorkspaceDirectory, preview.ActorId!, preview.ActorKind!,
                preview.ActorRole!, preview.Rationale!, preview.OccurredAt);
            var prepared = PrepareHandoff(request);
            if (!Same(preview, prepared.Preview))
                return FailedHandoffCommit(
                    ResearchWorkspaceOperationStatus.Stale,
                    ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                    "stale-screening-handoff-preview: authority, outcomes, actor, or rationale changed.");
            var commit = Commit(
                prepared.State, prepared.State.Conduct.Entries, prepared.Handoff, faultInjector);
            return new ResearchWorkspaceScreeningHandoffCommitResult(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                commit.AlreadyApplied ? "Screening handoff was already published." : "Screening handoff published.",
                commit.Project,
                prepared.Handoff.HandoffId,
                prepared.Handoff.Digest.ToString(),
                commit.AlreadyApplied);
        }
        catch (Exception exception) when (exception is ArgumentException or ScreeningRuleException)
        {
            return FailedHandoffCommit(
                ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "stale-screening-handoff-preview: handoff material no longer reproduces.");
        }
        catch (Exception exception)
        {
            var classified = Classify(exception);
            return FailedHandoffCommit(classified.Status, classified.ExitCode, classified.Message);
        }
    }

    private static PreparedResolution PrepareResolution(
        ResearchWorkspaceScreeningResolutionRequest request)
    {
        var state = Load(request.WorkingDirectory);
        if (state.Conduct.Handoff is not null)
            throw new ArgumentException(
                "Published Screening handoff is terminal; a canonical handoff invalidation " +
                "must be implemented before later correction or adjudication.");
        var candidateId = Required(request.CandidateId, nameof(request.CandidateId));
        if (!state.Conduct.Header.CandidateIds.Contains(candidateId, StringComparer.Ordinal))
            throw new ArgumentException("Candidate must identify an exact current Screening target.");
        var actor = Actor(request.ActorId, request.ActorKind, request.ActorRole);
        var kind = ParseResolutionKind(request.DecisionKind);
        var targetDigest = TargetDigest(state, candidateId);
        var targetSummaryDigest = TargetSummaryDigest(state, candidateId);
        var supersedes = OptionalDigest(request.SupersedesDecisionDigest);
        var conflictId = Optional(request.ResolvedConflictId);
        var sourceDigests = (request.SourceDecisionDigests ?? [])
            .Select(ContentDigest.Parse)
            .Distinct()
            .OrderBy(item => item.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (kind == ScreeningConductDecisionKind.Correction)
        {
            if (supersedes is null)
                throw new ArgumentException("Correction requires the exact superseded decision digest.");
            if (conflictId is not null || sourceDigests.Length != 0)
                throw new ArgumentException("Correction cannot identify adjudication conflict sources.");
        }
        else
        {
            if (supersedes is not null)
                throw new ArgumentException("Adjudication cannot supersede a reviewer decision.");
            if (conflictId is null || sourceDigests.Length == 0)
                throw new ArgumentException("Adjudication requires the exact conflict and source decision digests.");
        }
        var previous = state.Conduct.Journal.Projection.HeadDigest;
        var requestId = "screening-resolution-" + ContentDigest.Sha256CanonicalJson(
            new CanonicalJsonObject()
                .Add("workspace_id", state.Project.WorkspaceId)
                .Add("project_revision", state.Project.Revision)
                .Add("candidate_id", candidateId)
                .Add("target_summary_digest", targetSummaryDigest.ToString())
                .Add("kind", Kind(kind))
                .Add("verdict", Required(request.Verdict, nameof(request.Verdict)))
                .Add("actor", actor.ToCanonicalJson())
                .AddTimestamp("occurred_at", request.OccurredAt)).Value[7..23];
        var decision = ScreeningConductDecision.Create(
            state.Conduct.Header, state.Conduct.Entries.Count + 1, previous, requestId,
            candidateId, kind, Required(request.Verdict, nameof(request.Verdict)), actor,
            Required(request.Rationale, nameof(request.Rationale)), request.OccurredAt,
            Optional(request.ExclusionReasonCode), supersedes?.ToString(), conflictId, sourceDigests);
        var journal = ScreeningConductJournal.RehydrateEntries(
            state.Conduct.Header, state.Conduct.Policy,
            state.Conduct.Entries.Append<IScreeningConductEntry>(decision));
        var effects = kind == ScreeningConductDecisionKind.Correction
            ? CorrectionEffects
            : AdjudicationEffects;
        var token = ResolutionToken(
            state, candidateId, targetDigest, targetSummaryDigest, decision, effects);
        var preview = new ResearchWorkspaceScreeningResolutionPreview(
            ResearchWorkspaceOperationStatus.Succeeded,
            ResearchWorkspaceExitCodes.Success,
            "Review the exact Screening resolution authority effects before confirmation.",
            state.Location.RootDirectory,
            state.Project.WorkspaceId,
            state.Project.Revision,
            state.Package.Manifest.GenerationId,
            state.Project.ScreeningAuthorityPackageManifestSha256,
            state.Package.Manifest.SourceResultDigest,
            state.Package.Manifest.SourceSnapshotRecordDigest,
            state.Package.Manifest.DecisionSetDigest,
            state.Package.Manifest.ProtocolContentDigest,
            state.Package.Manifest.CriteriaDigest,
            state.Conduct.CorpusBinding!.BindingDigest.ToString(),
            state.Conduct.Manifest.GenerationId,
            state.Project.ScreeningConductManifestSha256,
            state.Conduct.Policy.PolicyId,
            state.Conduct.Policy.Digest.ToString(),
            state.Conduct.Header.Digest.ToString(),
            previous.ToString(),
            journal.Projection.HeadDigest.ToString(),
            state.Conduct.Entries.Count,
            candidateId,
            targetDigest.ToString(),
            targetSummaryDigest.ToString(),
            Kind(kind),
            decision.Verdict,
            actor.ActorId,
            actor.Kind,
            actor.Role,
            decision.Rationale,
            decision.ExclusionReasonCode,
            decision.SupersedesDecisionDigest?.ToString(),
            decision.ResolvedConflictId,
            decision.SourceDecisionDigests.Select(item => item.ToString()).ToArray(),
            request.OccurredAt,
            decision.DecisionId,
            decision.Digest.ToString(),
            effects,
            token);
        return new PreparedResolution(state, decision, preview);
    }

    private static PreparedHandoff PrepareHandoff(
        ResearchWorkspaceScreeningHandoffRequest request)
    {
        var state = Load(request.WorkingDirectory);
        var actor = Actor(request.ActorId, request.ActorKind, request.ActorRole);
        if (!state.Conduct.Policy.Authorizes(actor))
            throw new ScreeningRuleException(
                ScreeningErrorCodes.UnauthorizedReviewer,
                "Handoff actor is not a member of the verified Screening policy.");
        var rationale = Required(request.Rationale, nameof(request.Rationale));
        if (!state.Conduct.Journal.Projection.HandoffReady)
            throw new ScreeningRuleException(
                ScreeningErrorCodes.InsufficientReview,
                "Screening conduct is not ready for handoff.");
        var summaries = state.Conduct.Header.CandidateIds
            .Select(candidateId => TargetSummaryDigest(state, candidateId).ToString())
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var handoffId = "screening-handoff-" + ContentDigest.Sha256CanonicalJson(
            new CanonicalJsonObject()
                .Add("workspace_id", state.Project.WorkspaceId)
                .Add("journal_head_digest", state.Conduct.Journal.Projection.HeadDigest.ToString())
                .Add("target_summary_digests", new CanonicalJsonArray(summaries.Select(CanonicalJsonValue.From)))
                .Add("actor", actor.ToCanonicalJson())
                .Add("rationale", rationale)
                .AddTimestamp("occurred_at", request.OccurredAt)).Value[7..23];
        var confirmationMaterialDigest = ContentDigest.Sha256CanonicalJson(
            CommonToken(state, "handoff-publication")
                .Add("journal_head_digest",
                    state.Conduct.Journal.Projection.HeadDigest.ToString())
                .Add("target_summary_digests", new CanonicalJsonArray(
                    summaries.Select(CanonicalJsonValue.From)))
                .Add("actor", actor.ToCanonicalJson())
                .Add("rationale", rationale)
                .AddTimestamp("occurred_at", request.OccurredAt)
                .Add("effects", new CanonicalJsonArray(
                    HandoffEffects.Select(CanonicalJsonValue.From))));
        var handoff = ScreeningConductHandoff.Create(
            handoffId, state.Conduct.Journal, actor, rationale,
            confirmationMaterialDigest, request.OccurredAt);
        var token = HandoffToken(state, summaries, actor, rationale, handoff);
        var preview = new ResearchWorkspaceScreeningHandoffPreview(
            ResearchWorkspaceOperationStatus.Succeeded,
            ResearchWorkspaceExitCodes.Success,
            "Review the exact Screening handoff authority effects before confirmation.",
            state.Location.RootDirectory,
            state.Project.WorkspaceId,
            state.Project.Revision,
            state.Package.Manifest.GenerationId,
            state.Project.ScreeningAuthorityPackageManifestSha256,
            state.Package.Manifest.SourceResultDigest,
            state.Package.Manifest.SourceSnapshotRecordDigest,
            state.Package.Manifest.DecisionSetDigest,
            state.Package.Manifest.ProtocolContentDigest,
            state.Package.Manifest.CriteriaDigest,
            state.Conduct.CorpusBinding!.BindingDigest.ToString(),
            state.Conduct.Manifest.GenerationId,
            state.Project.ScreeningConductManifestSha256,
            state.Conduct.Policy.PolicyId,
            state.Conduct.Policy.Digest.ToString(),
            state.Conduct.Header.Digest.ToString(),
            state.Conduct.Journal.Projection.HeadDigest.ToString(),
            summaries,
            actor.ActorId,
            actor.Kind,
            actor.Role,
            rationale,
            request.OccurredAt,
            handoff.HandoffId,
            handoff.Digest.ToString(),
            HandoffEffects,
            token);
        return new PreparedHandoff(state, handoff, preview);
    }

    private static ResearchWorkspaceScreeningConductCommit Commit(
        State state,
        IReadOnlyList<IScreeningConductEntry> entries,
        ScreeningConductHandoff? handoff,
        Action<ResearchWorkspaceAuthorityFaultPoint>? faultInjector) =>
        ResearchWorkspaceScreeningConductTransaction.Commit(
            state.Location,
            state.Project,
            state.Package.Deduplication,
            state.Package.Protocol,
            state.Package.Criteria,
            state.Conduct.Policy,
            state.Conduct.Header,
            entries,
            handoff,
            faultInjector: faultInjector,
            corpusBinding: state.Conduct.CorpusBinding,
            sourceAuthority: state.Package.SourceResultAuthority,
            corpusSnapshot: state.Package.DeduplicationAuthorityChain.CurrentSnapshot);

    private static State Load(string workingDirectory)
    {
        var package = ResearchWorkspaceScreeningAuthorityPackage.VerifyCurrent(workingDirectory);
        var location = ResearchWorkspaceStore.FindFrom(Path.GetFullPath(
            Required(workingDirectory, nameof(workingDirectory))))
            ?? throw new ResearchWorkspaceMissingInputException(
                "No Nexus research workspace was found.");
        var project = ResearchWorkspaceStore.ReadProject(location.ProjectFilePath);
        var conduct = ResearchWorkspaceScreeningConductVerifier.VerifyCurrent(
            location, project, package.Deduplication, package.Protocol, package.Criteria,
            package.SourceResultAuthority, package.DeduplicationAuthorityChain.CurrentSnapshot);
        if (conduct.CorpusBinding is null)
            throw new InvalidOperationException(
                "Screening conduct lacks a verified corpus binding.");
        _ = ScreeningCorpusBindingAuthority.VerifyConductPolicyBinding(
            conduct.CorpusBinding, conduct.Policy);
        return new State(location, project, package, conduct);
    }

    private static ScreeningConductActor Actor(
        string actorId,
        string actorKind,
        string actorRole)
    {
        var actor = new ScreeningConductActor(
            Required(actorId, nameof(actorId)),
            Required(actorKind, nameof(actorKind)),
            Required(actorRole, nameof(actorRole)));
        if (!string.Equals(actor.Kind, ScreeningConductActorKinds.Human, StringComparison.Ordinal))
            throw new ScreeningRuleException(
                ScreeningErrorCodes.UnauthorizedReviewer,
                "Screening resolution and handoff require an explicit human actor.");
        return actor;
    }

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

    private static ContentDigest TargetSummaryDigest(State state, string candidateId)
    {
        var current = CurrentDecisionDigests(state.Conduct.Journal);
        var decisions = state.Conduct.Journal.Decisions
            .Where(item => item.CandidateId == candidateId && current.Contains(item.Digest))
            .OrderBy(item => item.Digest.ToString(), StringComparer.Ordinal)
            .Select(item => CanonicalJsonValue.From(item.Digest.ToString()))
            .ToArray();
        var conflicts = state.Conduct.Journal.Projection.Conflicts
            .Where(item => item.CandidateId == candidateId && !item.Resolved)
            .OrderBy(item => item.ConflictId, StringComparer.Ordinal)
            .Select(item => (CanonicalJsonValue)new CanonicalJsonObject()
                .Add("conflict_id", item.ConflictId)
                .Add("source_decision_digests", new CanonicalJsonArray(
                    item.SourceDecisionDigests
                        .OrderBy(value => value.ToString(), StringComparer.Ordinal)
                        .Select(value => CanonicalJsonValue.From(value.ToString())))))
            .ToArray();
        state.Conduct.Journal.Projection.Outcomes.TryGetValue(candidateId, out var outcome);
        var material = new CanonicalJsonObject()
            .Add("schema", "nexus.workspace-screening-target-summary")
            .Add("schema_version", "1.0.0")
            .Add("target_digest", TargetDigest(state, candidateId).ToString())
            .Add("journal_head_digest", state.Conduct.Journal.Projection.HeadDigest.ToString())
            .Add("current_decision_digests", new CanonicalJsonArray(decisions))
            .Add("unresolved_conflicts", new CanonicalJsonArray(conflicts));
        if (outcome is not null)
        {
            material
                .Add("outcome_verdict", outcome.Verdict)
                .Add("outcome_supporting_decision_digests", new CanonicalJsonArray(
                    outcome.SupportingDecisionDigests
                        .OrderBy(value => value.ToString(), StringComparer.Ordinal)
                        .Select(value => CanonicalJsonValue.From(value.ToString()))));
            if (outcome.ExclusionReasonCode is not null)
                material.Add("outcome_exclusion_reason_code", outcome.ExclusionReasonCode);
        }
        return ContentDigest.Sha256CanonicalJson(material);
    }

    private static HashSet<ContentDigest> CurrentDecisionDigests(
        ScreeningConductJournal journal)
    {
        var superseded = journal.Decisions
            .Where(item => item.Kind == ScreeningConductDecisionKind.Correction &&
                item.SupersedesDecisionDigest is not null)
            .Select(item => item.SupersedesDecisionDigest!.Value);
        return journal.Decisions.Select(item => item.Digest)
            .Except(superseded)
            .Except(journal.Projection.InvalidatedDecisionDigests)
            .ToHashSet();
    }

    private static string ResolutionToken(
        State state,
        string candidateId,
        ContentDigest targetDigest,
        ContentDigest targetSummaryDigest,
        ScreeningConductDecision decision,
        IReadOnlyList<string> effects) =>
        ContentDigest.Sha256CanonicalJson(CommonToken(state, "resolution")
            .Add("candidate_id", candidateId)
            .Add("target_digest", targetDigest.ToString())
            .Add("target_summary_digest", targetSummaryDigest.ToString())
            .Add("decision_digest", decision.Digest.ToString())
            .Add("actor", decision.Actor.ToCanonicalJson())
            .Add("rationale", decision.Rationale)
            .Add("source_decision_digests", new CanonicalJsonArray(
                decision.SourceDecisionDigests.Select(value =>
                    CanonicalJsonValue.From(value.ToString()))))
            .Add("supersedes_decision_digest",
                decision.SupersedesDecisionDigest is null
                    ? CanonicalJsonValue.Null()
                    : CanonicalJsonValue.From(decision.SupersedesDecisionDigest.Value.ToString()))
            .Add("resolved_conflict_id",
                decision.ResolvedConflictId is null
                    ? CanonicalJsonValue.Null()
                    : CanonicalJsonValue.From(decision.ResolvedConflictId))
            .AddTimestamp("occurred_at", decision.DecidedAt)
            .Add("effects", new CanonicalJsonArray(effects.Select(CanonicalJsonValue.From))))
        .ToString();

    private static string HandoffToken(
        State state,
        IReadOnlyList<string> targetSummaryDigests,
        ScreeningConductActor actor,
        string rationale,
        ScreeningConductHandoff handoff) =>
        ContentDigest.Sha256CanonicalJson(CommonToken(state, "handoff")
            .Add("journal_head_digest", state.Conduct.Journal.Projection.HeadDigest.ToString())
            .Add("target_summary_digests", new CanonicalJsonArray(
                targetSummaryDigests.Select(CanonicalJsonValue.From)))
            .Add("actor", actor.ToCanonicalJson())
            .Add("rationale", rationale)
            .Add("handoff_digest", handoff.Digest.ToString())
            .AddTimestamp("occurred_at", handoff.CreatedAt)
            .Add("effects", new CanonicalJsonArray(HandoffEffects.Select(CanonicalJsonValue.From))))
        .ToString();

    private static CanonicalJsonObject CommonToken(State state, string operation) =>
        new CanonicalJsonObject()
            .Add("schema", "nexus.workspace-screening-resolution-preview")
            .Add("schema_version", "1.0.0")
            .Add("operation", operation)
            .Add("workspace_id", state.Project.WorkspaceId)
            .Add("project_revision", state.Project.Revision)
            .Add("authority_generation", state.Package.Manifest.GenerationId)
            .Add("authority_manifest_digest", state.Project.ScreeningAuthorityPackageManifestSha256!)
            .Add("source_result_digest", state.Package.Manifest.SourceResultDigest)
            .Add("source_snapshot_record_digest", state.Package.Manifest.SourceSnapshotRecordDigest)
            .Add("decision_set_digest", state.Package.Manifest.DecisionSetDigest)
            .Add("protocol_content_digest", state.Package.Manifest.ProtocolContentDigest)
            .Add("criteria_digest", state.Package.Manifest.CriteriaDigest)
            .Add("corpus_binding_digest", state.Conduct.CorpusBinding!.BindingDigest.ToString())
            .Add("conduct_generation", state.Conduct.Manifest.GenerationId)
            .Add("conduct_manifest_digest", state.Project.ScreeningConductManifestSha256!)
            .Add("policy_digest", state.Conduct.Policy.Digest.ToString())
            .Add("header_digest", state.Conduct.Header.Digest.ToString());

    private static bool Same(
        ResearchWorkspaceScreeningResolutionPreview left,
        ResearchWorkspaceScreeningResolutionPreview right) =>
        left == right ||
        left with
        {
            SourceDecisionDigests = right.SourceDecisionDigests,
            ExpectedEffects = right.ExpectedEffects
        } == right &&
        left.SourceDecisionDigests.SequenceEqual(
            right.SourceDecisionDigests, StringComparer.Ordinal) &&
        left.ExpectedEffects.SequenceEqual(right.ExpectedEffects, StringComparer.Ordinal);

    private static bool Same(
        ResearchWorkspaceScreeningHandoffPreview left,
        ResearchWorkspaceScreeningHandoffPreview right) =>
        left == right ||
        left with
        {
            TargetSummaryDigests = right.TargetSummaryDigests,
            ExpectedEffects = right.ExpectedEffects
        } == right &&
        left.TargetSummaryDigests.SequenceEqual(
            right.TargetSummaryDigests, StringComparer.Ordinal) &&
        left.ExpectedEffects.SequenceEqual(right.ExpectedEffects, StringComparer.Ordinal);

    private static ScreeningConductDecisionKind ParseResolutionKind(string value) =>
        Required(value, nameof(value)).ToLowerInvariant() switch
        {
            "correction" => ScreeningConductDecisionKind.Correction,
            "adjudication" => ScreeningConductDecisionKind.Adjudication,
            _ => throw new ArgumentException(
                "Slice 6 admits correction and adjudication decisions only.")
        };

    private static string Kind(ScreeningConductDecisionKind value) =>
        value.ToString().ToLowerInvariant();

    private static ContentDigest? OptionalDigest(string? value)
    {
        value = Optional(value);
        return value is null ? null : ContentDigest.Parse(value);
    }

    private static ResearchWorkspaceScreeningResolutionPreview FailedResolutionPreview(
        ResearchWorkspaceScreeningResolutionRequest request,
        Classification failure) => new(
        failure.Status, failure.ExitCode, failure.Message,
        Path.GetFullPath(request.WorkingDirectory),
        null, null, null, null, null, null, null, null, null, null, null, null,
        null, null, null, null, null, 0,
        request.CandidateId, null, null, request.DecisionKind, request.Verdict,
        request.ActorId, request.ActorKind, request.ActorRole, request.Rationale,
        request.ExclusionReasonCode, request.SupersedesDecisionDigest,
        request.ResolvedConflictId, request.SourceDecisionDigests ?? [],
        request.OccurredAt, null, null, [], null);

    private static ResearchWorkspaceScreeningHandoffPreview FailedHandoffPreview(
        ResearchWorkspaceScreeningHandoffRequest request,
        Classification failure) => new(
        Status: failure.Status,
        ExitCode: failure.ExitCode,
        Message: failure.Message,
        WorkspaceDirectory: Path.GetFullPath(request.WorkingDirectory),
        WorkspaceId: null,
        ExpectedProjectRevision: null,
        AuthorityPackageGenerationId: null,
        AuthorityPackageManifestDigest: null,
        SourceResultDigest: null,
        SourceSnapshotRecordDigest: null,
        DecisionSetDigest: null,
        ProtocolContentDigest: null,
        CriteriaDigest: null,
        CorpusBindingDigest: null,
        ConductGenerationId: null,
        ConductManifestDigest: null,
        PolicyId: null,
        PolicyDigest: null,
        HeaderDigest: null,
        JournalHeadDigest: null,
        TargetSummaryDigests: [],
        ActorId: request.ActorId,
        ActorKind: request.ActorKind,
        ActorRole: request.ActorRole,
        Rationale: request.Rationale,
        OccurredAt: request.OccurredAt,
        HandoffId: null,
        HandoffDigest: null,
        ExpectedEffects: [],
        ConfirmationToken: null);

    private static ResearchWorkspaceScreeningResolutionCommitResult FailedResolutionCommit(
        ResearchWorkspaceOperationStatus status,
        int exitCode,
        string message) => new(status, exitCode, message, null, null, null, false);

    private static ResearchWorkspaceScreeningHandoffCommitResult FailedHandoffCommit(
        ResearchWorkspaceOperationStatus status,
        int exitCode,
        string message) => new(status, exitCode, message, null, null, null, false);

    private static Classification Classify(Exception exception) => exception switch
    {
        ResearchWorkspaceScreeningAuthorityException authority
            when authority.Category == ResearchWorkspaceScreeningAuthorityPackage.StaleCategory =>
            new(ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure, authority.Message),
        ResearchWorkspaceConcurrencyException concurrency
            when concurrency.InnerException is not IOException =>
            new(ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure, concurrency.Message),
        IOException or UnauthorizedAccessException =>
            new(ResearchWorkspaceOperationStatus.RecoveryRequired,
                ResearchWorkspaceExitCodes.UnexpectedRuntimeFailure,
                "Screening resolution could not access the local workspace safely."),
        ResearchWorkspaceMissingInputException =>
            new(ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.MissingProjectOrInput, exception.Message),
        ArgumentException or ScreeningRuleException =>
            new(ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure, exception.Message),
        _ => new(ResearchWorkspaceOperationStatus.RecoveryRequired,
            ResearchWorkspaceExitCodes.UnexpectedRuntimeFailure,
            "Screening resolution authority could not be reconstructed from the local workspace.")
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

    private sealed record PreparedResolution(
        State State,
        ScreeningConductDecision Decision,
        ResearchWorkspaceScreeningResolutionPreview Preview);

    private sealed record PreparedHandoff(
        State State,
        ScreeningConductHandoff Handoff,
        ResearchWorkspaceScreeningHandoffPreview Preview);

    private sealed record Classification(
        ResearchWorkspaceOperationStatus Status,
        int ExitCode,
        string Message);
}
