using System.Globalization;
using NexusScholar.AppServices;
using NexusScholar.Bundles;
using NexusScholar.Kernel;
using NexusScholar.Reporting;
using NexusScholar.Screening;

namespace NexusScholar.ResearchWorkspace;

public sealed record ResearchWorkspaceReviewExportRequest(
    string WorkingDirectory,
    string ExportId,
    string ActorId,
    string ActorRole,
    DateTimeOffset OccurredAt,
    IReadOnlyList<string> Disclosures,
    IReadOnlyList<string> NonClaims);

public sealed record ResearchWorkspaceReviewFlowCounts(
    int Identified,
    int DuplicatesConsolidated,
    int PostDedup,
    int TitleAbstractIncluded,
    int TitleAbstractExcluded,
    int FullTextIncluded,
    int FullTextExcluded,
    int Included);

public sealed record ResearchWorkspaceReviewExportPreview(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    string WorkspaceDirectory,
    string? WorkspaceId,
    long? ExpectedProjectRevision,
    string? ExportId,
    string? ActorId,
    string? ActorRole,
    DateTimeOffset OccurredAt,
    string? ReportDigest,
    string? SliceDigest,
    string? WorkspaceCutDigest,
    string? BundleManifestDigest,
    string? InventoryDigest,
    string? ExportRequestDigest,
    string? ExpectedPreviousLedgerEntryDigest,
    ResearchWorkspaceReviewFlowCounts? Counts,
    IReadOnlyList<string> Disclosures,
    IReadOnlyList<string> NonClaims,
    IReadOnlyList<string> ExpectedEffects,
    string? ConfirmationToken)
{
    public bool IsReady => Status == ResearchWorkspaceOperationStatus.Succeeded &&
        ConfirmationToken is not null && ExportRequestDigest is not null;
}

public sealed record ResearchWorkspaceReviewExportCommitResult(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    string? ExportId,
    string? EntryDigest,
    long? Ordinal,
    bool AlreadyApplied,
    bool RoundTripVerified)
{
    public bool Completed => Status == ResearchWorkspaceOperationStatus.Succeeded;
}

public static class ResearchWorkspaceReviewExportWorkflow
{
    private static readonly string[] Effects =
    [
        "finalize one canonical review-flow report from verified authorities",
        "build one exact-inventory Bundle v2",
        "append one human-authorized immutable export ledger entry",
        "round-trip verify report, bundle, and export ledger"
    ];

    public static ResearchWorkspaceReviewExportPreview Preview(
        ResearchWorkspaceReviewExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return Prepare(request).Preview;
        }
        catch (Exception exception)
        {
            return FailedPreview(request, exception.Message);
        }
    }

    public static ResearchWorkspaceReviewExportCommitResult Commit(
        ResearchWorkspaceReviewExportPreview preview,
        Action<ResearchWorkspaceExportFaultPoint>? faultInjector = null)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.IsReady)
            return FailedCommit("An exact successful report/export preview is required.");
        try
        {
            var prepared = Prepare(new ResearchWorkspaceReviewExportRequest(
                preview.WorkspaceDirectory, preview.ExportId!, preview.ActorId!,
                preview.ActorRole!, preview.OccurredAt, preview.Disclosures, preview.NonClaims));
            if (!Same(preview, prepared.Preview))
                return new ResearchWorkspaceReviewExportCommitResult(
                    ResearchWorkspaceOperationStatus.Stale,
                    ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                    "stale-report-export-preview: source authority, project revision, or ledger head changed.",
                    null, null, null, false, false);
            var expectedPrevious = preview.ExpectedPreviousLedgerEntryDigest is null
                ? (ContentDigest?)null
                : ContentDigest.Parse(preview.ExpectedPreviousLedgerEntryDigest);
            var commit = ResearchWorkspaceExportTransaction.Commit(
                prepared.Location, prepared.Project, prepared.ExportRequest,
                expectedPrevious, faultInjector: faultInjector);
            var replay = ResearchWorkspaceExportLedgerVerifier.Replay(prepared.Location);
            var roundTrip = replay.Head?.EntryDigest == commit.Entry.Digest &&
                replay.Entries.Any(item => item.ExportId == commit.Entry.ExportId);
            if (!roundTrip)
                throw new InvalidOperationException("Published export did not round-trip through ledger verification.");
            return new ResearchWorkspaceReviewExportCommitResult(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                commit.AlreadyApplied ? "Review export was already published and verified." :
                    "Review report, Bundle v2, and export ledger entry were published and verified.",
                commit.Entry.ExportId, commit.Entry.Digest.ToString(), commit.Entry.Ordinal,
                commit.AlreadyApplied, true);
        }
        catch (Exception exception)
        {
            return ClassifyCommit(exception);
        }
    }

    private static Prepared Prepare(ResearchWorkspaceReviewExportRequest request)
    {
        var package = ResearchWorkspaceScreeningAuthorityPackage.VerifyCurrent(request.WorkingDirectory);
        var location = ResearchWorkspaceStore.FindFrom(Path.GetFullPath(request.WorkingDirectory))
            ?? throw new ResearchWorkspaceMissingInputException("No Nexus research workspace was found.");
        var project = ResearchWorkspaceStore.ReadProject(location.ProjectFilePath);
        var screening = ResearchWorkspaceScreeningConductVerifier.VerifyCurrent(
            location, project, package.Deduplication, package.Protocol, package.Criteria,
            package.SourceResultAuthority, package.DeduplicationAuthorityChain.CurrentSnapshot);
        if (screening.Handoff is null || screening.CorpusBinding is null)
            throw new InvalidOperationException(
                "Final reporting requires a verified snapshot-bound title/abstract handoff.");
        var reportingWorkflow = ResearchWorkspaceReportingWorkflow.VerifyCurrent(
            location, project, package.Protocol);
        var titleIncludes = screening.Journal.Projection.Outcomes.Values
            .Where(item => item.Verdict == ScreeningVerdicts.Include)
            .Select(item => item.CandidateId)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        var fullTextCases = new List<FullTextReviewCaseAuthorities>();
        foreach (var candidateId in titleIncludes)
        {
            if (project.FullTextCases?.ContainsKey(candidateId) != true)
                throw new InvalidOperationException(
                    $"Title/abstract include '{candidateId}' has no current Full Text generation.");
            var generation = ResearchWorkspaceFullTextGenerationVerifier.VerifyCurrent(
                location, project, candidateId, screening.Journal, screening.Handoff,
                50L * 1024 * 1024, expectedConductPolicy: null,
                package.Deduplication, package.Protocol);
            if (generation.ConductJournal is null || generation.ConductHandoff is null)
                throw new InvalidOperationException(
                    $"Full Text case '{candidateId}' has no terminal verified conduct handoff.");
            fullTextCases.Add(new FullTextReviewCaseAuthorities(
                generation.Admission, generation.Authority, generation.ConductJournal,
                generation.ConductHandoff, generation.ExtractionAttempt));
        }
        var generationBindings = new List<ReviewGenerationBinding>
        {
            Binding(ReviewGenerationRoles.Protocol,
                package.Manifest.GenerationId, project.ScreeningAuthorityPackageManifestSha256!),
            Binding(ReviewGenerationRoles.Workflow,
                reportingWorkflow.Manifest.GenerationId, project.ReportingWorkflowManifestSha256!),
            Binding(ReviewGenerationRoles.Deduplication,
                project.CurrentAuthorityGenerationId!, project.AuthorityGenerationManifestSha256!),
            Binding(ReviewGenerationRoles.CorpusSnapshot,
                package.Manifest.GenerationId, project.ScreeningAuthorityPackageManifestSha256!),
            Binding(ReviewGenerationRoles.ScreeningConduct,
                screening.Manifest.GenerationId, project.ScreeningConductManifestSha256!)
        };
        generationBindings.AddRange(titleIncludes.Select(candidateId =>
        {
            var pointer = project.FullTextCases![candidateId];
            return new ReviewGenerationBinding(
                ReviewGenerationRoles.FullText, pointer.GenerationId,
                ContentDigest.Parse(pointer.ManifestSha256), candidateId);
        }));
        var cut = VerifiedReviewWorkspaceCut.FromVerifiedGenerations(
            project.WorkspaceId, project.Revision, generationBindings);
        var authorities = new ReviewSliceAuthorities(
            package.Protocol,
            VerifiedReportingWorkflowAuthority.FromVerified(reportingWorkflow.Workflow),
            package.SourceResultAuthority,
            package.DeduplicationAuthorityChain.CurrentSnapshot,
            NexusScholar.Screening.CorpusSnapshots.VerifiedSnapshotBoundScreeningPolicy.FromVerified(
                screening.CorpusBinding, screening.Policy),
            screening.Journal,
            screening.Handoff,
            fullTextCases,
            [], [], [], [], cut);
        var projection = ReviewFlowProjector.Project(
            authorities, request.Disclosures, request.NonClaims);
        var report = ReviewFlowProjector.Finalize(projection);
        var reportBytes = ReportingCanonicalCodec.SerializeReport(report);
        var sources = cut.Generations.Select(item => new BundleV2SourceBinding(
            item.Role, item.GenerationId,
            new BundleV2ScopedDigest(DigestScope.CanonicalJsonRecord.ToString(), item.ManifestDigest),
            item.CandidateId)).ToArray();
        var reportSource = sources.First(item => item.Role == ReviewGenerationRoles.Protocol);
        var bundle = ReviewBundleV2Authority.Create(
            $"bundle-{Required(request.ExportId)}",
            new BundleV2ScopedDigest(DigestScope.CanonicalJsonRecord.ToString(), report.ReportDigest),
            project.WorkspaceId, project.Revision,
            new BundleV2ScopedDigest(DigestScope.CanonicalJsonRecord.ToString(), cut.Digest),
            sources,
            [new BundleV2EmbeddedEntry(
                1, "reports/report.json", reportBytes.LongLength,
                new BundleV2ScopedDigest(DigestScope.RawArtifactBytes.ToString(), ContentDigest.Sha256(reportBytes)),
                "canonical-report", reportSource)],
            ["Bundle identity excludes archive transport metadata.", "No external compatibility certification."]);
        var manifestBytes = ReviewBundleV2CanonicalCodec.Serialize(bundle);
        var inventory = new[]
        {
            new BundleV2ObservedEntry(BundleV2Constants.ManifestPath, manifestBytes),
            new BundleV2ObservedEntry("reports/report.json", reportBytes)
        };
        var occurredAt = request.OccurredAt.UtcDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var exportRequest = ReviewExportOrchestrator.Prepare(
            Required(request.ExportId),
            new ReviewExportActor(
                Required(request.ActorId), ReviewExportActorKinds.Human,
                Required(request.ActorRole)),
            occurredAt, report, bundle, manifestBytes, inventory);
        var ledger = ResearchWorkspaceExportLedgerVerifier.Replay(location);
        var previous = ledger.Head?.EntryDigest.ToString();
        var preview = new ResearchWorkspaceReviewExportPreview(
            ResearchWorkspaceOperationStatus.Succeeded,
            ResearchWorkspaceExitCodes.Success,
            "Review the exact report, Bundle v2, and export publication effects before confirmation.",
            location.RootDirectory, project.WorkspaceId, project.Revision,
            exportRequest.ExportId, exportRequest.Actor, exportRequest.ActorRole, request.OccurredAt,
            report.ReportDigest.ToString(), report.SliceDigest.ToString(), cut.Digest.ToString(),
            bundle.ManifestDigest.ToString(), exportRequest.ObservedInventoryDigest.ToString(),
            exportRequest.RequestDigest.ToString(), previous,
            new ResearchWorkspaceReviewFlowCounts(
                report.Projection.Counts.Identified,
                report.Projection.Counts.DuplicatesConsolidated,
                report.Projection.Counts.PostDedup,
                report.Projection.Counts.TitleAbstractIncluded,
                report.Projection.Counts.TitleAbstractExcluded,
                report.Projection.Counts.FullTextIncluded,
                report.Projection.Counts.FullTextExcluded,
                report.Projection.Counts.Included),
            request.Disclosures.ToArray(), request.NonClaims.ToArray(), Effects,
            Token(project, exportRequest, previous, request.Disclosures, request.NonClaims));
        return new Prepared(location, project, exportRequest, preview);
    }

    private static ReviewGenerationBinding Binding(string role, string generationId, string digest) =>
        new(role, generationId, ContentDigest.Parse(digest));

    private static string Token(
        ResearchWorkspaceProject project,
        VerifiedReviewExportRequest request,
        string? previous,
        IReadOnlyList<string> disclosures,
        IReadOnlyList<string> nonClaims) =>
        ContentDigest.Sha256CanonicalJson(new CanonicalJsonObject()
            .Add("schema", "nexus.workspace-review-export-preview")
            .Add("schema_version", "1.0.0")
            .Add("workspace_id", project.WorkspaceId)
            .Add("project_revision", project.Revision)
            .Add("export_request_digest", request.RequestDigest.ToString())
            .Add("expected_previous_entry_digest", previous is null
                ? CanonicalJsonValue.Null() : CanonicalJsonValue.From(previous))
            .Add("disclosures", CanonicalJsonValue.Array(disclosures.Select(CanonicalJsonValue.From).ToArray()))
            .Add("non_claims", CanonicalJsonValue.Array(nonClaims.Select(CanonicalJsonValue.From).ToArray()))
            .Add("effects", CanonicalJsonValue.Array(Effects.Select(CanonicalJsonValue.From).ToArray())))
        .ToString();

    private static bool Same(
        ResearchWorkspaceReviewExportPreview left,
        ResearchWorkspaceReviewExportPreview right) =>
        left.WorkspaceDirectory == right.WorkspaceDirectory &&
        left.WorkspaceId == right.WorkspaceId &&
        left.ExpectedProjectRevision == right.ExpectedProjectRevision &&
        left.ExportId == right.ExportId &&
        left.ActorId == right.ActorId &&
        left.ActorRole == right.ActorRole &&
        left.OccurredAt == right.OccurredAt &&
        left.ReportDigest == right.ReportDigest &&
        left.SliceDigest == right.SliceDigest &&
        left.WorkspaceCutDigest == right.WorkspaceCutDigest &&
        left.BundleManifestDigest == right.BundleManifestDigest &&
        left.InventoryDigest == right.InventoryDigest &&
        left.ExportRequestDigest == right.ExportRequestDigest &&
        left.ExpectedPreviousLedgerEntryDigest == right.ExpectedPreviousLedgerEntryDigest &&
        left.Disclosures.SequenceEqual(right.Disclosures) &&
        left.NonClaims.SequenceEqual(right.NonClaims) &&
        left.ExpectedEffects.SequenceEqual(right.ExpectedEffects) &&
        string.Equals(left.ConfirmationToken, right.ConfirmationToken, StringComparison.Ordinal);

    private static string Required(string value) =>
        !string.IsNullOrWhiteSpace(value) ? value.Trim() :
        throw new ArgumentException("A non-empty value is required.");

    private static ResearchWorkspaceReviewExportPreview FailedPreview(
        ResearchWorkspaceReviewExportRequest request,
        string message) => new(
        ResearchWorkspaceOperationStatus.Failed,
        ResearchWorkspaceExitCodes.UsageOrValidationFailure,
        message, request.WorkingDirectory, null, null, request.ExportId, request.ActorId,
        request.ActorRole,
        request.OccurredAt, null, null, null, null, null, null, null, null,
        request.Disclosures, request.NonClaims, Effects, null);

    private static ResearchWorkspaceReviewExportCommitResult FailedCommit(string message) =>
        new(ResearchWorkspaceOperationStatus.Failed,
            ResearchWorkspaceExitCodes.UsageOrValidationFailure,
            message, null, null, null, false, false);

    private static ResearchWorkspaceReviewExportCommitResult ClassifyCommit(Exception exception)
    {
        var status = exception switch
        {
            WorkspaceExportException export when export.Category is
                WorkspaceExportErrorCodes.StaleHead or
                WorkspaceExportErrorCodes.SourceDrift or
                WorkspaceExportErrorCodes.ExportCollision =>
                ResearchWorkspaceOperationStatus.Stale,
            ArgumentException or FormatException =>
                ResearchWorkspaceOperationStatus.Failed,
            _ => ResearchWorkspaceOperationStatus.RecoveryRequired
        };
        var exitCode = status == ResearchWorkspaceOperationStatus.RecoveryRequired
            ? ResearchWorkspaceExitCodes.UnexpectedRuntimeFailure
            : ResearchWorkspaceExitCodes.UsageOrValidationFailure;
        return new ResearchWorkspaceReviewExportCommitResult(
            status, exitCode, exception.Message, null, null, null, false, false);
    }

    private sealed record Prepared(
        ResearchWorkspaceLocation Location,
        ResearchWorkspaceProject Project,
        VerifiedReviewExportRequest ExportRequest,
        ResearchWorkspaceReviewExportPreview Preview);
}
