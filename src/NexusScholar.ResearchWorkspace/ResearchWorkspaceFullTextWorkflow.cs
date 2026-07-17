using NexusScholar.FullText;
using NexusScholar.Kernel;
using NexusScholar.Screening;
using NexusScholar.Screening.FullText;

namespace NexusScholar.ResearchWorkspace;

public sealed record ResearchWorkspaceFullTextIntakeRequest(
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

public sealed record ResearchWorkspaceFullTextIntakePreview(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    string WorkspaceDirectory,
    string? WorkspaceId,
    long? ExpectedProjectRevision,
    string? ScreeningAuthorityManifestDigest,
    string? ScreeningConductManifestDigest,
    string? ScreeningHandoffDigest,
    string? CandidateId,
    string? LocalPath,
    string? ArtifactKind,
    string? MediaType,
    string? ActorId,
    string? ActorKind,
    DateTimeOffset OccurredAt,
    long MaximumBytes,
    string? ExpectedSupersededManifestDigest,
    string? AdmissionDigest,
    string? InputDigest,
    string? AcquisitionDigest,
    string? ArtifactEvidenceDigest,
    string? RawArtifactDigest,
    string? ExtractionAttemptDigest,
    string? ExtractionStatus,
    string? ResultingGenerationId,
    IReadOnlyList<string> ExpectedEffects,
    string? ConfirmationToken)
{
    public bool IsReady => Status == ResearchWorkspaceOperationStatus.Succeeded &&
        ConfirmationToken is not null && ResultingGenerationId is not null;
}

public sealed record ResearchWorkspaceFullTextIntakeCommitResult(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    ResearchWorkspaceProject? Project,
    string? CandidateId,
    string? GenerationId,
    string? RawArtifactDigest,
    string? ExtractionStatus,
    bool AlreadyApplied)
{
    public bool Completed => Status == ResearchWorkspaceOperationStatus.Succeeded;
}

public sealed record ResearchWorkspaceFullTextReviewRequest(
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

public sealed record ResearchWorkspaceFullTextReviewPreview(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    string WorkspaceDirectory,
    string? WorkspaceId,
    long? ExpectedProjectRevision,
    string? ScreeningAuthorityManifestDigest,
    string? ScreeningConductManifestDigest,
    string? ScreeningHandoffDigest,
    string? FullTextGenerationId,
    string? FullTextManifestDigest,
    string? CandidateId,
    string? AdmissionDigest,
    string? RawArtifactDigest,
    string? ExtractionAttemptDigest,
    string? ExtractionStatus,
    string? CriteriaDigest,
    string? PolicyDigest,
    string? HeaderDigest,
    string? DecisionDigest,
    string? ResultingHeadDigest,
    string? Verdict,
    string? ActorId,
    string? ActorKind,
    string? ActorRole,
    string? Rationale,
    string? InclusionCriteria,
    string? ExclusionCriteria,
    string? ExclusionReasonCode,
    string? SelectedExclusionReasonCode,
    DateTimeOffset OccurredAt,
    IReadOnlyList<string> ExpectedEffects,
    string? ConfirmationToken)
{
    public bool IsReady => Status == ResearchWorkspaceOperationStatus.Succeeded &&
        ConfirmationToken is not null && DecisionDigest is not null;
}

public sealed record ResearchWorkspaceFullTextReviewCommitResult(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    ResearchWorkspaceProject? Project,
    string? CandidateId,
    string? DecisionDigest,
    string? HeadDigest,
    bool HandoffReady,
    bool AlreadyApplied)
{
    public bool Completed => Status == ResearchWorkspaceOperationStatus.Succeeded;
}

public static class ResearchWorkspaceFullTextWorkflow
{
    private static readonly string[] IntakeEffects =
    [
        "read one local Full Text file without network access",
        "persist raw bytes and canonical acquisition evidence",
        "persist one deterministic extraction attempt",
        "publish one immutable Full Text generation",
        "advance the workspace project revision"
    ];

    private static readonly string[] ReviewEffects =
    [
        "append one human Full Text Screening decision",
        "bind the decision to the exact raw artifact and extraction attempt",
        "publish one immutable Full Text successor generation",
        "advance the workspace project revision"
    ];

    public static ResearchWorkspaceFullTextIntakePreview PreviewIntake(
        ResearchWorkspaceFullTextIntakeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return PrepareIntake(request).Preview;
        }
        catch (Exception exception)
        {
            return FailedIntakePreview(request, Classify(exception));
        }
    }

    public static ResearchWorkspaceFullTextIntakeCommitResult CommitIntake(
        ResearchWorkspaceFullTextIntakePreview preview,
        Action<ResearchWorkspaceAuthorityFaultPoint>? faultInjector = null)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.IsReady)
            return FailedIntakeCommit(
                ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "An exact successful local Full Text intake preview is required.");
        try
        {
            var request = new ResearchWorkspaceFullTextIntakeRequest(
                preview.WorkspaceDirectory, preview.CandidateId!, preview.LocalPath!,
                preview.ArtifactKind!, preview.MediaType!, preview.ActorId!,
                preview.ActorKind!, preview.OccurredAt, preview.MaximumBytes,
                preview.ExpectedSupersededManifestDigest);
            var prepared = PrepareIntake(request);
            if (!Same(preview, prepared.Preview))
                return FailedIntakeCommit(
                    ResearchWorkspaceOperationStatus.Stale,
                    ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                    "stale-fulltext-intake-preview: local bytes, extraction, or authority changed.");
            var commit = ResearchWorkspaceFullTextTransaction.Commit(
                prepared.State.Location,
                prepared.State.Project,
                prepared.State.Screening.Journal,
                prepared.State.Screening.Handoff!,
                prepared.Admission,
                prepared.Authority,
                prepared.RawBytes,
                preview.MaximumBytes,
                prepared.Extraction,
                faultInjector: faultInjector);
            return new ResearchWorkspaceFullTextIntakeCommitResult(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                commit.AlreadyApplied ? "Local Full Text intake was already applied." : "Local Full Text intake committed.",
                commit.Project,
                prepared.Admission.CandidateId,
                commit.Manifest.GenerationId,
                prepared.Authority.Artifact.RawByteDigest,
                prepared.Extraction.Status,
                commit.AlreadyApplied);
        }
        catch (Exception exception) when (
            exception is ArgumentException or FullTextRuleException or ScreeningRuleException)
        {
            return FailedIntakeCommit(
                ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "stale-fulltext-intake-preview: intake material no longer reproduces.");
        }
        catch (Exception exception)
        {
            var classified = Classify(exception);
            return FailedIntakeCommit(classified.Status, classified.ExitCode, classified.Message);
        }
    }

    public static ResearchWorkspaceFullTextReviewPreview PreviewReview(
        ResearchWorkspaceFullTextReviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return PrepareReview(request).Preview;
        }
        catch (Exception exception)
        {
            return FailedReviewPreview(request, Classify(exception));
        }
    }

    public static ResearchWorkspaceFullTextReviewCommitResult CommitReview(
        ResearchWorkspaceFullTextReviewPreview preview,
        Action<ResearchWorkspaceAuthorityFaultPoint>? faultInjector = null)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.IsReady)
            return FailedReviewCommit(
                ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "An exact successful Full Text review preview is required.");
        try
        {
            var request = new ResearchWorkspaceFullTextReviewRequest(
                preview.WorkspaceDirectory, preview.Verdict!, preview.ActorId!,
                preview.ActorKind!, preview.ActorRole!, preview.Rationale!,
                preview.InclusionCriteria!, preview.ExclusionCriteria!,
                preview.ExclusionReasonCode!, preview.SelectedExclusionReasonCode,
                preview.OccurredAt, preview.CandidateId);
            var prepared = PrepareReview(request);
            if (!Same(preview, prepared.Preview))
                return FailedReviewCommit(
                    ResearchWorkspaceOperationStatus.Stale,
                    ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                    "stale-fulltext-review-preview: artifact, extraction, actor, criteria, or authority changed.");
            var records = ConductRecords(
                prepared.Policy, prepared.Header, [prepared.Decision], prepared.Journal);
            var commit = ResearchWorkspaceFullTextTransaction.Commit(
                prepared.State.Location,
                prepared.State.Project,
                prepared.State.Screening.Journal,
                prepared.State.Screening.Handoff!,
                prepared.State.Generation.Admission,
                prepared.State.Generation.Authority,
                prepared.State.RawBytes,
                prepared.State.MaximumBytes,
                prepared.State.Generation.ExtractionAttempt,
                records,
                prepared.Policy,
                faultInjector);
            return new ResearchWorkspaceFullTextReviewCommitResult(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                commit.AlreadyApplied ? "Full Text review was already applied." : "Full Text review committed.",
                commit.Project,
                prepared.State.Generation.Admission.CandidateId,
                prepared.Decision.Digest.ToString(),
                prepared.Journal.Projection.HeadDigest.ToString(),
                prepared.Journal.Projection.HandoffReady,
                commit.AlreadyApplied);
        }
        catch (Exception exception) when (
            exception is ArgumentException or FullTextRuleException or ScreeningRuleException)
        {
            return FailedReviewCommit(
                ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                "stale-fulltext-review-preview: decision material no longer reproduces.");
        }
        catch (Exception exception)
        {
            var classified = Classify(exception);
            return FailedReviewCommit(classified.Status, classified.ExitCode, classified.Message);
        }
    }

    private static PreparedIntake PrepareIntake(
        ResearchWorkspaceFullTextIntakeRequest request)
    {
        if (request.MaximumBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.MaximumBytes));
        var state = LoadScreening(request.WorkingDirectory);
        if (state.Screening.Handoff is null)
            throw new InvalidOperationException(
                "A verified current title/abstract Screening handoff is required.");
        var candidateId = Required(request.CandidateId, nameof(request.CandidateId));
        var admission = VerifiedFullTextAdmission.Create(
            state.Screening.Journal, state.Screening.Handoff, candidateId);
        ValidatePredecessor(state, candidateId, request.ExpectedSupersededManifestDigest);
        var rawBytes = ResearchWorkspaceLocalFullTextSource.ReadBytes(
            request.LocalPath, request.MaximumBytes);
        var rawDigest = ContentDigest.Sha256(rawBytes);
        var artifactKind = Required(request.ArtifactKind, nameof(request.ArtifactKind));
        var mediaType = Required(request.MediaType, nameof(request.MediaType));
        var actor = new FullTextActor(
            Required(request.ActorId, nameof(request.ActorId)),
            Required(request.ActorKind, nameof(request.ActorKind)));
        if (!string.Equals(actor.ActorKind, FullTextActorKinds.Human, StringComparison.Ordinal))
            throw new FullTextRuleException(
                FullTextErrorCodes.MissingHumanOrImportActor,
                "Desktop local intake requires an explicit human actor.");
        var seed = ContentDigest.Sha256Utf8(
            $"{admission.Digest}|{rawDigest}|{artifactKind}|{mediaType}");
        var acquisitionId = $"fulltext-acquisition-{seed.Value[7..23]}";
        var artifactId = $"fulltext-artifact-{seed.Value[23..39]}";
        var acquisition = FullTextAcquisitionRecord.CreateLocal(
            acquisitionId, admission.Input, Path.GetFullPath(request.LocalPath),
            actor, request.OccurredAt, artifactId, artifactKind, mediaType);
        var artifact = FullTextArtifactEvidence.FromBytes(
            artifactId, admission.Input, acquisition, artifactKind, mediaType,
            rawBytes, request.MaximumBytes,
            originalFileName: Path.GetFileName(request.LocalPath));
        var authority = FullTextRehydrator.Rehydrate(
            new UnverifiedFullTextChain(
                admission.Input, acquisition, artifact, rawBytes, request.MaximumBytes));
        var extraction = FullTextDeterministicExtractor.Extract(
            $"fulltext-extraction-{seed.Value[39..55]}",
            authority,
            rawBytes,
            request.OccurredAt);
        var inputDigest = ContentDigest.Sha256(
            FullTextAuthorityCanonicalCodec.Serialize(authority.Input));
        var acquisitionDigest = ContentDigest.Sha256(
            FullTextAuthorityCanonicalCodec.Serialize(authority.Acquisition));
        var artifactDigest = ContentDigest.Sha256(
            FullTextAuthorityCanonicalCodec.Serialize(authority.Artifact));
        var generationId = GenerationId(
            admission, authority, extraction, rawBytes, additionalRecords: []);
        var preview = new ResearchWorkspaceFullTextIntakePreview(
            ResearchWorkspaceOperationStatus.Succeeded,
            ResearchWorkspaceExitCodes.Success,
            "Review the exact local Full Text intake effects before confirmation.",
            state.Location.RootDirectory,
            state.Project.WorkspaceId,
            state.Project.Revision,
            state.Project.ScreeningAuthorityPackageManifestSha256,
            state.Project.ScreeningConductManifestSha256,
            state.Screening.Handoff.Digest.ToString(),
            candidateId,
            Path.GetFullPath(request.LocalPath),
            artifactKind,
            mediaType,
            actor.ActorId,
            actor.ActorKind,
            request.OccurredAt,
            request.MaximumBytes,
            Optional(request.ExpectedSupersededManifestDigest),
            admission.Digest.ToString(),
            inputDigest.ToString(),
            acquisitionDigest.ToString(),
            artifactDigest.ToString(),
            rawDigest.ToString(),
            extraction.Digest.ToString(),
            extraction.Status,
            generationId,
            IntakeEffects,
            IntakeToken(
                state, candidateId, Path.GetFullPath(request.LocalPath), actor,
                request, admission, inputDigest, acquisitionDigest, artifactDigest,
                rawDigest, extraction, generationId));
        return new PreparedIntake(
            state, admission, authority, extraction, rawBytes, preview);
    }

    private static PreparedReview PrepareReview(
        ResearchWorkspaceFullTextReviewRequest request)
    {
        var state = LoadFullText(request.WorkingDirectory, Optional(request.CandidateId));
        if (state.Generation.ConductJournal is not null)
            throw new InvalidOperationException(
                "The current Slice 7 review surface accepts the initial Full Text decision only.");
        var actor = new ScreeningConductActor(
            Required(request.ActorId, nameof(request.ActorId)),
            Required(request.ActorKind, nameof(request.ActorKind)),
            Required(request.ActorRole, nameof(request.ActorRole)));
        if (!string.Equals(actor.Kind, ScreeningConductActorKinds.Human, StringComparison.Ordinal))
            throw new ScreeningRuleException(
                ScreeningErrorCodes.AutomationCannotFinalize,
                "Full Text Screening decisions require an explicit human actor.");
        var inclusion = Required(request.InclusionCriteria, nameof(request.InclusionCriteria));
        var exclusion = Required(request.ExclusionCriteria, nameof(request.ExclusionCriteria));
        var reason = Required(request.ExclusionReasonCode, nameof(request.ExclusionReasonCode));
        var criteriaSeed = ContentDigest.Sha256CanonicalJson(new CanonicalJsonObject()
            .Add("admission_digest", state.Generation.Admission.Digest.ToString())
            .Add("inclusion", inclusion)
            .Add("exclusion", exclusion));
        var criteria = new ScreeningCriteria(
            $"criteria-fulltext-{criteriaSeed.Value[7..19]}",
            "1.0.0",
            ScreeningStages.FullText,
            CanonicalJsonValue.From(inclusion),
            CanonicalJsonValue.From(exclusion),
            true,
            state.Package.Protocol.Version.Id,
            state.Package.Protocol.Version.ContentDigest.ToString(),
            approvedProtocolDigestScope: DigestScope.ProtocolContent.ToString(),
            approvedProtocolStatus: ScreeningProtocolBindingStatus.Approved,
            currentProtocolContentDigest:
                state.Package.Protocol.Version.ContentDigest.ToString());
        var rawDigest = ContentDigest.Parse(
            state.Generation.Authority.Artifact.RawByteDigest);
        var extractionDigest = state.Generation.ExtractionAttempt?.Digest;
        var policy = FullTextScreeningConductPolicy.Create(
            $"fulltext-policy-{criteriaSeed.Value[19..31]}",
            state.Generation.Admission.CandidateSetId,
            state.Package.Deduplication,
            state.Package.Protocol,
            criteria,
            state.Generation.Admission,
            1,
            [new ScreeningConductRoleAssignment(actor.ActorId, actor.Role)],
            [actor.Role],
            [new ScreeningExclusionReason(reason, ScreeningStages.FullText)],
            actor,
            request.OccurredAt,
            rawDigest,
            extractionDigest);
        var header = FullTextScreeningConductHeader.Create(
            $"fulltext-conduct-{criteriaSeed.Value[31..43]}",
            policy,
            actor,
            request.OccurredAt);
        var evidence = new[]
        {
            new ScreeningConductEvidenceRef(
                FullTextScreeningConductEvidenceKinds.FullTextArtifact,
                state.Generation.Authority.Artifact.ArtifactId,
                rawDigest)
        };
        var decision = FullTextScreeningConductDecision.Create(
            header,
            1,
            header.Digest,
            $"fulltext-review-{criteriaSeed.Value[43..55]}",
            state.Generation.Admission.CandidateId,
            ScreeningConductDecisionKind.Review,
            Required(request.Verdict, nameof(request.Verdict)),
            actor,
            Required(request.Rationale, nameof(request.Rationale)),
            request.OccurredAt,
            Optional(request.SelectedExclusionReasonCode),
            evidence: evidence,
            extractionAttempt: state.Generation.ExtractionAttempt);
        var journal = FullTextScreeningConductJournal.RehydrateEntries(
            header, policy, [decision]);
        var preview = new ResearchWorkspaceFullTextReviewPreview(
            ResearchWorkspaceOperationStatus.Succeeded,
            ResearchWorkspaceExitCodes.Success,
            "Review the exact Full Text Screening effects before confirmation.",
            state.Location.RootDirectory,
            state.Project.WorkspaceId,
            state.Project.Revision,
            state.Project.ScreeningAuthorityPackageManifestSha256,
            state.Project.ScreeningConductManifestSha256,
            state.Screening.Handoff!.Digest.ToString(),
            state.Generation.Manifest.GenerationId,
            FullTextPointer(state).ManifestSha256,
            state.Generation.Admission.CandidateId,
            state.Generation.Admission.Digest.ToString(),
            rawDigest.ToString(),
            extractionDigest?.ToString(),
            state.Generation.ExtractionAttempt?.Status,
            criteria.ComputeDigest().ToString(),
            policy.Digest.ToString(),
            header.Digest.ToString(),
            decision.Digest.ToString(),
            journal.Projection.HeadDigest.ToString(),
            decision.Verdict,
            actor.ActorId,
            actor.Kind,
            actor.Role,
            decision.Rationale,
            inclusion,
            exclusion,
            reason,
            decision.ExclusionReasonCode,
            request.OccurredAt,
            ReviewEffects,
            ReviewToken(state, criteria, policy, header, decision, journal));
        return new PreparedReview(state, policy, header, decision, journal, preview);
    }

    private static ScreeningState LoadScreening(string workingDirectory)
    {
        var package = ResearchWorkspaceScreeningAuthorityPackage.VerifyCurrent(
            workingDirectory);
        var location = ResearchWorkspaceStore.FindFrom(Path.GetFullPath(
            Required(workingDirectory, nameof(workingDirectory))))
            ?? throw new ResearchWorkspaceMissingInputException(
                "No Nexus research workspace was found.");
        var project = ResearchWorkspaceStore.ReadProject(location.ProjectFilePath);
        var screening = ResearchWorkspaceScreeningConductVerifier.VerifyCurrent(
            location, project, package.Deduplication, package.Protocol, package.Criteria,
            package.SourceResultAuthority,
            package.DeduplicationAuthorityChain.CurrentSnapshot);
        return new ScreeningState(location, project, package, screening);
    }

    private static FullTextState LoadFullText(string workingDirectory, string? candidateId)
    {
        var state = LoadScreening(workingDirectory);
        if (state.Screening.Handoff is null)
            throw new InvalidOperationException(
                "A verified current title/abstract Screening handoff is required.");
        const long maximumBytes = 50L * 1024 * 1024;
        var generation = ResearchWorkspaceFullTextGenerationVerifier.VerifyCurrent(
            state.Location,
            state.Project,
            candidateId,
            state.Screening.Journal,
            state.Screening.Handoff,
            maximumBytes);
        var rawArtifact = generation.Manifest.Artifacts.Single(item =>
            item.Name == "raw-artifact");
        var rawBytes = File.ReadAllBytes(ResearchWorkspacePaths.InProject(
            state.Location.RootDirectory, rawArtifact.RelativePath));
        return new FullTextState(
            state.Location, state.Project, state.Package, state.Screening,
            generation, rawBytes, maximumBytes);
    }

    private static void ValidatePredecessor(
        ScreeningState state,
        string candidateId,
        string? expectedSupersededManifestDigest)
    {
        var expected = Optional(expectedSupersededManifestDigest);
        var predecessor = state.Project.FullTextCases?.GetValueOrDefault(candidateId);
        if (predecessor is null)
        {
            if (expected is not null)
                throw new InvalidOperationException(
                    "No current Full Text generation exists to supersede.");
            return;
        }
        if (expected is null ||
            !string.Equals(
                expected, predecessor.ManifestSha256,
                StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Duplicate candidate intake requires the exact current manifest digest as supersession authority.");
    }

    private static IReadOnlyList<ResearchWorkspaceFullTextRecord> ConductRecords(
        FullTextScreeningConductPolicy policy,
        FullTextScreeningConductHeader header,
        IReadOnlyList<IFullTextScreeningConductEntry> entries,
        FullTextScreeningConductJournal journal)
    {
        var records = new List<ResearchWorkspaceFullTextRecord>
        {
            new("criteria", ScreeningCriteriaCanonicalCodec.Serialize(policy.Criteria)),
            Record("conduct-policy", policy.ToCanonicalJson()),
            Record("conduct-header", header.ToCanonicalJson())
        };
        records.AddRange(entries.Select((entry, index) => new ResearchWorkspaceFullTextRecord(
            $"conduct-entry-{index + 1:D6}",
            entry switch
            {
                FullTextScreeningConductDecision decision =>
                    FullTextScreeningConductCanonicalCodec.Serialize(decision),
                FullTextScreeningConductInvalidation invalidation =>
                    FullTextScreeningConductCanonicalCodec.Serialize(invalidation),
                _ => throw new InvalidOperationException(
                    "Unknown Full Text conduct entry type.")
            })));
        if (journal.Projection.HandoffReady)
        {
            var handoff = journal.CreateHandoff(
                $"fulltext-handoff-{journal.Projection.HeadDigest.Value[7..23]}",
                entries.OfType<FullTextScreeningConductDecision>().Last().DecidedAt);
            records.Add(new ResearchWorkspaceFullTextRecord(
                "conduct-handoff",
                FullTextScreeningConductCanonicalCodec.Serialize(handoff)));
        }
        return records;
    }

    private static ResearchWorkspaceFullTextRecord Record(
        string name,
        CanonicalJsonObject value) =>
        new(name, CanonicalJsonSerializer.SerializeToUtf8Bytes(value));

    private static string GenerationId(
        VerifiedFullTextAdmission admission,
        VerifiedFullTextChain authority,
        FullTextExtractionAttempt extraction,
        byte[] rawBytes,
        IReadOnlyList<ResearchWorkspaceFullTextRecord> additionalRecords)
    {
        var records = new List<(string Name, byte[] Bytes)>
        {
            ("admission", VerifiedFullTextAdmissionCanonicalCodec.Serialize(admission)),
            ("input", FullTextAuthorityCanonicalCodec.Serialize(authority.Input)),
            ("acquisition", FullTextAuthorityCanonicalCodec.Serialize(authority.Acquisition)),
            ("artifact-evidence", FullTextAuthorityCanonicalCodec.Serialize(authority.Artifact)),
            ("raw-artifact", rawBytes),
            ("extraction-attempt", FullTextExtractionAttemptCodec.Serialize(extraction))
        };
        records.AddRange(additionalRecords.Select(item => (item.Name, item.CanonicalBytes)));
        var digest = ContentDigest.Sha256Utf8(string.Join("|",
            records.OrderBy(item => item.Name, StringComparer.Ordinal)
                .Select(item => $"{item.Name}:{ContentDigest.Sha256(item.Bytes)}")));
        return $"fulltext-{digest.Value[7..23]}";
    }

    private static string IntakeToken(
        ScreeningState state,
        string candidateId,
        string localPath,
        FullTextActor actor,
        ResearchWorkspaceFullTextIntakeRequest request,
        VerifiedFullTextAdmission admission,
        ContentDigest inputDigest,
        ContentDigest acquisitionDigest,
        ContentDigest artifactDigest,
        ContentDigest rawDigest,
        FullTextExtractionAttempt extraction,
        string generationId) =>
        ContentDigest.Sha256CanonicalJson(CommonToken(state, "intake")
            .Add("candidate_id", candidateId)
            .Add("local_path", localPath)
            .Add("artifact_kind", request.ArtifactKind)
            .Add("media_type", request.MediaType)
            .Add("actor_id", actor.ActorId)
            .Add("actor_kind", actor.ActorKind)
            .AddTimestamp("occurred_at", request.OccurredAt)
            .Add("maximum_bytes", request.MaximumBytes)
            .Add("expected_superseded_manifest_digest",
                request.ExpectedSupersededManifestDigest is null
                    ? CanonicalJsonValue.Null()
                    : CanonicalJsonValue.From(request.ExpectedSupersededManifestDigest))
            .Add("admission_digest", admission.Digest.ToString())
            .Add("input_digest", inputDigest.ToString())
            .Add("acquisition_digest", acquisitionDigest.ToString())
            .Add("artifact_evidence_digest", artifactDigest.ToString())
            .Add("raw_artifact_digest", rawDigest.ToString())
            .Add("extraction_attempt_digest", extraction.Digest.ToString())
            .Add("extraction_status", extraction.Status)
            .Add("resulting_generation_id", generationId)
            .Add("effects", new CanonicalJsonArray(
                IntakeEffects.Select(CanonicalJsonValue.From))))
        .ToString();

    private static string ReviewToken(
        FullTextState state,
        ScreeningCriteria criteria,
        FullTextScreeningConductPolicy policy,
        FullTextScreeningConductHeader header,
        FullTextScreeningConductDecision decision,
        FullTextScreeningConductJournal journal) =>
        ContentDigest.Sha256CanonicalJson(CommonToken(state, "review")
            .Add("fulltext_generation_id", state.Generation.Manifest.GenerationId)
            .Add("fulltext_manifest_digest", FullTextPointer(state).ManifestSha256)
            .Add("admission_digest", state.Generation.Admission.Digest.ToString())
            .Add("raw_artifact_digest", state.Generation.Authority.Artifact.RawByteDigest)
            .Add("extraction_attempt_digest",
                state.Generation.ExtractionAttempt is null
                    ? CanonicalJsonValue.Null()
                    : CanonicalJsonValue.From(
                        state.Generation.ExtractionAttempt.Digest.ToString()))
            .Add("extraction_status",
                state.Generation.ExtractionAttempt is null
                    ? CanonicalJsonValue.Null()
                    : CanonicalJsonValue.From(state.Generation.ExtractionAttempt.Status))
            .Add("criteria_digest", criteria.ComputeDigest().ToString())
            .Add("policy_digest", policy.Digest.ToString())
            .Add("header_digest", header.Digest.ToString())
            .Add("decision_digest", decision.Digest.ToString())
            .Add("resulting_head_digest", journal.Projection.HeadDigest.ToString())
            .Add("actor", decision.Actor.ToCanonicalJson())
            .Add("rationale", decision.Rationale)
            .AddTimestamp("occurred_at", decision.DecidedAt)
            .Add("effects", new CanonicalJsonArray(
                ReviewEffects.Select(CanonicalJsonValue.From))))
        .ToString();

    private static ResearchWorkspaceFullTextPointer FullTextPointer(FullTextState state) =>
        state.Project.FullTextCases?.GetValueOrDefault(state.Generation.Manifest.CandidateId)
        ?? new ResearchWorkspaceFullTextPointer(
            state.Project.CurrentFullTextGenerationId!,
            state.Project.FullTextManifestPath!,
            state.Project.FullTextManifestSha256!);

    private static CanonicalJsonObject CommonToken(
        ScreeningState state,
        string operation) =>
        new CanonicalJsonObject()
            .Add("schema", "nexus.workspace-fulltext-preview")
            .Add("schema_version", "1.0.0")
            .Add("operation", operation)
            .Add("workspace_id", state.Project.WorkspaceId)
            .Add("project_revision", state.Project.Revision)
            .Add("screening_authority_manifest_digest",
                state.Project.ScreeningAuthorityPackageManifestSha256!)
            .Add("screening_conduct_manifest_digest",
                state.Project.ScreeningConductManifestSha256!)
            .Add("screening_handoff_digest",
                state.Screening.Handoff!.Digest.ToString());

    private static CanonicalJsonObject CommonToken(
        FullTextState state,
        string operation) =>
        CommonToken(
            new ScreeningState(
                state.Location, state.Project, state.Package, state.Screening),
            operation);

    private static bool Same(
        ResearchWorkspaceFullTextIntakePreview left,
        ResearchWorkspaceFullTextIntakePreview right) =>
        left == right ||
        left with { ExpectedEffects = right.ExpectedEffects } == right &&
        left.ExpectedEffects.SequenceEqual(right.ExpectedEffects, StringComparer.Ordinal);

    private static bool Same(
        ResearchWorkspaceFullTextReviewPreview left,
        ResearchWorkspaceFullTextReviewPreview right) =>
        left == right ||
        left with { ExpectedEffects = right.ExpectedEffects } == right &&
        left.ExpectedEffects.SequenceEqual(right.ExpectedEffects, StringComparer.Ordinal);

    private static ResearchWorkspaceFullTextIntakePreview FailedIntakePreview(
        ResearchWorkspaceFullTextIntakeRequest request,
        Classification failure) => new(
        failure.Status, failure.ExitCode, failure.Message,
        Path.GetFullPath(request.WorkingDirectory),
        null, null, null, null, null,
        request.CandidateId, request.LocalPath, request.ArtifactKind, request.MediaType,
        request.ActorId, request.ActorKind, request.OccurredAt, request.MaximumBytes,
        request.ExpectedSupersededManifestDigest,
        null, null, null, null, null, null, null, null, [], null);

    private static ResearchWorkspaceFullTextReviewPreview FailedReviewPreview(
        ResearchWorkspaceFullTextReviewRequest request,
        Classification failure) => new(
        Status: failure.Status,
        ExitCode: failure.ExitCode,
        Message: failure.Message,
        WorkspaceDirectory: Path.GetFullPath(request.WorkingDirectory),
        WorkspaceId: null,
        ExpectedProjectRevision: null,
        ScreeningAuthorityManifestDigest: null,
        ScreeningConductManifestDigest: null,
        ScreeningHandoffDigest: null,
        FullTextGenerationId: null,
        FullTextManifestDigest: null,
        CandidateId: null,
        AdmissionDigest: null,
        RawArtifactDigest: null,
        ExtractionAttemptDigest: null,
        ExtractionStatus: null,
        CriteriaDigest: null,
        PolicyDigest: null,
        HeaderDigest: null,
        DecisionDigest: null,
        ResultingHeadDigest: null,
        Verdict: request.Verdict,
        ActorId: request.ActorId,
        ActorKind: request.ActorKind,
        ActorRole: request.ActorRole,
        Rationale: request.Rationale,
        InclusionCriteria: request.InclusionCriteria,
        ExclusionCriteria: request.ExclusionCriteria,
        ExclusionReasonCode: request.ExclusionReasonCode,
        SelectedExclusionReasonCode: request.SelectedExclusionReasonCode,
        OccurredAt: request.OccurredAt,
        ExpectedEffects: [],
        ConfirmationToken: null);

    private static ResearchWorkspaceFullTextIntakeCommitResult FailedIntakeCommit(
        ResearchWorkspaceOperationStatus status,
        int exitCode,
        string message) => new(
        status, exitCode, message, null, null, null, null, null, false);

    private static ResearchWorkspaceFullTextReviewCommitResult FailedReviewCommit(
        ResearchWorkspaceOperationStatus status,
        int exitCode,
        string message) => new(
        status, exitCode, message, null, null, null, null, false, false);

    private static Classification Classify(Exception exception) => exception switch
    {
        ResearchWorkspaceScreeningAuthorityException authority
            when authority.Category ==
                ResearchWorkspaceScreeningAuthorityPackage.StaleCategory =>
            new(ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                authority.Message),
        ResearchWorkspaceConcurrencyException concurrency
            when concurrency.InnerException is not IOException =>
            new(ResearchWorkspaceOperationStatus.Stale,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                concurrency.Message),
        IOException or UnauthorizedAccessException =>
            new(ResearchWorkspaceOperationStatus.RecoveryRequired,
                ResearchWorkspaceExitCodes.UnexpectedRuntimeFailure,
                "Full Text workflow could not access the local workspace safely."),
        ResearchWorkspaceMissingInputException =>
            new(ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.MissingProjectOrInput, exception.Message),
        ArgumentException or InvalidOperationException or FullTextRuleException or
            ScreeningRuleException =>
            new(ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure, exception.Message),
        _ => new(ResearchWorkspaceOperationStatus.RecoveryRequired,
            ResearchWorkspaceExitCodes.UnexpectedRuntimeFailure,
            "Full Text workflow authority could not be reconstructed.")
    };

    private static string Required(string? value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        return value.Trim();
    }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ScreeningState(
        ResearchWorkspaceLocation Location,
        ResearchWorkspaceProject Project,
        VerifiedResearchWorkspaceScreeningAuthorityPackage Package,
        VerifiedResearchWorkspaceScreeningConduct Screening);

    private sealed record FullTextState(
        ResearchWorkspaceLocation Location,
        ResearchWorkspaceProject Project,
        VerifiedResearchWorkspaceScreeningAuthorityPackage Package,
        VerifiedResearchWorkspaceScreeningConduct Screening,
        VerifiedResearchWorkspaceFullTextGeneration Generation,
        byte[] RawBytes,
        long MaximumBytes);

    private sealed record PreparedIntake(
        ScreeningState State,
        VerifiedFullTextAdmission Admission,
        VerifiedFullTextChain Authority,
        FullTextExtractionAttempt Extraction,
        byte[] RawBytes,
        ResearchWorkspaceFullTextIntakePreview Preview);

    private sealed record PreparedReview(
        FullTextState State,
        FullTextScreeningConductPolicy Policy,
        FullTextScreeningConductHeader Header,
        FullTextScreeningConductDecision Decision,
        FullTextScreeningConductJournal Journal,
        ResearchWorkspaceFullTextReviewPreview Preview);

    private sealed record Classification(
        ResearchWorkspaceOperationStatus Status,
        int ExitCode,
        string Message);
}
