using System.Text.Json;
using NexusScholar.Kernel;
using NexusScholar.Protocol;
using NexusScholar.Workflow;

namespace NexusScholar.ResearchWorkspace;

public sealed record ResearchWorkspaceReportingWorkflowManifest(
    string Schema,
    string GenerationId,
    string WorkspaceId,
    long ProjectRevision,
    string WorkflowId,
    string WorkflowDigest,
    string ProtocolVersionId,
    string ProtocolContentDigest,
    string TemplateId,
    string TemplateVersion,
    string TemplateDigest)
{
    public const string CurrentSchema = "nexus.workspace-reporting-workflow-generation.v1";
}

public sealed record VerifiedResearchWorkspaceReportingWorkflow(
    ResearchWorkspaceReportingWorkflowManifest Manifest,
    VerifiedWorkflowDefinition Workflow);

public sealed record ResearchWorkspaceReportingWorkflowPreview(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    string WorkspaceDirectory,
    string? WorkspaceId,
    long? ExpectedProjectRevision,
    string? ProtocolContentDigest,
    string? ResultingGenerationId,
    string? ResultingManifestDigest,
    IReadOnlyList<string> ExpectedEffects,
    string? ConfirmationToken)
{
    public bool IsReady => Status == ResearchWorkspaceOperationStatus.Succeeded &&
        ConfirmationToken is not null;
}

public sealed record ResearchWorkspaceReportingWorkflowCommitResult(
    ResearchWorkspaceOperationStatus Status,
    int ExitCode,
    string Message,
    ResearchWorkspaceProject? Project,
    string? GenerationId,
    bool AlreadyApplied)
{
    public bool Completed => Status == ResearchWorkspaceOperationStatus.Succeeded;
}

public static class ResearchWorkspaceReportingWorkflow
{
    private const string ManifestFileName = "reporting-workflow.manifest.json";
    private static readonly string[] Effects =
    [
        "persist one deterministic protocol-bound reporting Workflow authority",
        "advance the workspace project revision"
    ];

    public static ResearchWorkspaceReportingWorkflowPreview Preview(string workingDirectory)
    {
        try
        {
            return Prepare(workingDirectory).Preview;
        }
        catch (Exception exception)
        {
            return new ResearchWorkspaceReportingWorkflowPreview(
                ResearchWorkspaceOperationStatus.Failed,
                ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                exception.Message, workingDirectory, null, null, null, null, null,
                Effects, null);
        }
    }

    public static ResearchWorkspaceReportingWorkflowCommitResult Commit(
        ResearchWorkspaceReportingWorkflowPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.IsReady)
            return Failed("An exact successful reporting Workflow preview is required.");
        try
        {
            var prepared = Prepare(preview.WorkspaceDirectory);
            if (!Same(prepared.Preview, preview))
                return new ResearchWorkspaceReportingWorkflowCommitResult(
                    ResearchWorkspaceOperationStatus.Stale,
                    ResearchWorkspaceExitCodes.UsageOrValidationFailure,
                    "stale-reporting-workflow-preview: Protocol or project authority changed.",
                    null, null, false);
            var alreadyApplied =
                prepared.Project.CurrentReportingWorkflowGenerationId ==
                prepared.Preview.ResultingGenerationId;
            var project = Commit(
                prepared.Location, prepared.Project, prepared.Protocol);
            return new ResearchWorkspaceReportingWorkflowCommitResult(
                ResearchWorkspaceOperationStatus.Succeeded,
                ResearchWorkspaceExitCodes.Success,
                alreadyApplied ? "Reporting Workflow authority was already current." :
                    "Reporting Workflow authority committed.",
                project, project.CurrentReportingWorkflowGenerationId, alreadyApplied);
        }
        catch (Exception exception)
        {
            return Failed(exception.Message);
        }
    }

    public static VerifiedResearchWorkspaceReportingWorkflow VerifyCurrent(
        ResearchWorkspaceLocation location,
        ResearchWorkspaceProject project,
        VerifiedProtocolVersion protocol)
    {
        if (project.CurrentReportingWorkflowGenerationId is null ||
            project.ReportingWorkflowManifestPath is null ||
            project.ReportingWorkflowManifestSha256 is null)
            throw new InvalidOperationException("The workspace has no current reporting Workflow authority.");
        var path = Resolve(location, project.ReportingWorkflowManifestPath);
        var bytes = File.ReadAllBytes(path);
        if (ContentDigest.Sha256(bytes).ToString() != project.ReportingWorkflowManifestSha256)
            throw new InvalidOperationException("Reporting Workflow manifest failed pointer digest verification.");
        var manifest = Rehydrate(bytes);
        var workflow = Build(protocol);
        if (manifest.GenerationId != project.CurrentReportingWorkflowGenerationId ||
            manifest.WorkspaceId != project.WorkspaceId ||
            manifest.ProjectRevision > project.Revision ||
            manifest.WorkflowId != workflow.Definition.WorkflowId ||
            manifest.WorkflowDigest != workflow.Definition.WorkflowDigest.ToString() ||
            manifest.ProtocolVersionId != protocol.Version.Id ||
            manifest.ProtocolContentDigest != protocol.Version.ContentDigest.ToString() ||
            manifest.TemplateId != workflow.ResolvedTemplate.TemplateId ||
            manifest.TemplateVersion != workflow.ResolvedTemplate.TemplateVersion ||
            manifest.TemplateDigest != workflow.ResolvedTemplate.TemplateDigest.ToString())
            throw new InvalidOperationException("Reporting Workflow manifest is stale or does not reproduce.");
        return new VerifiedResearchWorkspaceReportingWorkflow(manifest, workflow);
    }

    public static ResearchWorkspaceProject Commit(
        ResearchWorkspaceLocation location,
        ResearchWorkspaceProject expectedProject,
        VerifiedProtocolVersion protocol)
    {
        var workflow = Build(protocol);
        if (expectedProject.CurrentReportingWorkflowGenerationId is not null)
        {
            var current = VerifyCurrent(location, expectedProject, protocol);
            if (current.Workflow.Definition.WorkflowDigest == workflow.Definition.WorkflowDigest)
                return expectedProject;
        }
        var generationId = $"reporting-workflow-{workflow.Definition.WorkflowDigest.Value[7..23]}";
        var relativeRoot = ResearchWorkspacePaths.ReportingWorkflowGenerationRoot(generationId);
        var manifestPath = $"{relativeRoot}/{ManifestFileName}";
        var placeholder = "sha256:" + new string('0', 64);
        var committed = expectedProject.CommitReportingWorkflowGeneration(generationId, manifestPath, placeholder);
        var manifest = new ResearchWorkspaceReportingWorkflowManifest(
            ResearchWorkspaceReportingWorkflowManifest.CurrentSchema,
            generationId, expectedProject.WorkspaceId, committed.Revision,
            workflow.Definition.WorkflowId, workflow.Definition.WorkflowDigest.ToString(),
            protocol.Version.Id, protocol.Version.ContentDigest.ToString(),
            workflow.ResolvedTemplate.TemplateId, workflow.ResolvedTemplate.TemplateVersion,
            workflow.ResolvedTemplate.TemplateDigest.ToString());
        var bytes = Serialize(manifest);
        committed = committed with { ReportingWorkflowManifestSha256 = ContentDigest.Sha256(bytes).ToString() };
        var finalRoot = ResearchWorkspacePaths.InProject(location.RootDirectory, relativeRoot);
        var stagingRoot = ResearchWorkspacePaths.InProject(location.RootDirectory,
            $"{ResearchWorkspacePaths.GenerationStaging}/{generationId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingRoot);
        try
        {
            File.WriteAllBytes(Path.Combine(stagingRoot, ManifestFileName), bytes);
            using var workspaceLock = new FileStream(
                Path.Combine(location.RootDirectory, ResearchWorkspacePaths.ProjectLockFileName),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var current = ResearchWorkspaceStore.ReadProject(location.ProjectFilePath);
            if (ResearchWorkspaceJson.Serialize(current) != ResearchWorkspaceJson.Serialize(expectedProject))
                throw new ResearchWorkspaceConcurrencyException(expectedProject.Revision, current.Revision);
            if (Directory.Exists(finalRoot))
                throw new ResearchWorkspaceConcurrencyException(
                    "Reporting Workflow generation identity collision.", new InvalidOperationException());
            Directory.CreateDirectory(Path.GetDirectoryName(finalRoot)!);
            Directory.Move(stagingRoot, finalRoot);
            try
            {
                ResearchWorkspaceStore.WriteProject(location, committed);
            }
            catch
            {
                var quarantine = ResearchWorkspacePaths.InProject(location.RootDirectory,
                    $"{ResearchWorkspacePaths.GenerationQuarantine}/{generationId}-{Guid.NewGuid():N}");
                Directory.CreateDirectory(Path.GetDirectoryName(quarantine)!);
                Directory.Move(finalRoot, quarantine);
                throw;
            }
        }
        finally
        {
            if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, true);
        }
        _ = VerifyCurrent(location, committed, protocol);
        return committed;
    }

    public static VerifiedWorkflowDefinition Build(VerifiedProtocolVersion protocol)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        var template = new WorkflowTemplate(
            "nexus-local-review-reporting", "1.0.0", ContentDigest.Sha256Utf8("placeholder"),
            "nexus.workflow-template", "1.0.0", [],
            [new WorkflowTemplateNode(
                "review-report", WorkflowNodeKind.HumanTask, WorkflowNodeMode.Human,
                "Review and publish report", [], [], "approve-report", [], null, null)],
            [], [],
            [new WorkflowTemplateApprovalRequirement(
                "approve-report", "human-report-publication", "1.0.0",
                "single_researcher", ["reviewer"], 1, false, false)],
            [new WorkflowTemplateRole("reviewer", "Reviewer", "Human report publication authority")],
            [], [], [], []);
        template = template with { TemplateDigest = WorkflowCompiler.ComputeLocalTemplateDigest(template) };
        var compiled = new WorkflowCompiler().Compile(new WorkflowCompileInput(
            protocol, template, new Dictionary<string, CanonicalJsonValue>(),
            [new WorkflowSchemaRef("nexus.workflow-template", "1.0.0"),
             new WorkflowSchemaRef("nexus.workflow-definition", "1.1.0")]));
        return WorkflowRehydrator.Rehydrate(
            WorkflowRehydrator.FromCompiled(compiled),
            new Resolver(protocol, template));
    }

    private static PreparedPreview Prepare(string workingDirectory)
    {
        var package = ResearchWorkspaceScreeningAuthorityPackage.VerifyCurrent(workingDirectory);
        var location = ResearchWorkspaceStore.FindFrom(Path.GetFullPath(workingDirectory))
            ?? throw new ResearchWorkspaceMissingInputException("No Nexus research workspace was found.");
        var project = ResearchWorkspaceStore.ReadProject(location.ProjectFilePath);
        var workflow = Build(package.Protocol);
        var generationId = $"reporting-workflow-{workflow.Definition.WorkflowDigest.Value[7..23]}";
        if (project.CurrentReportingWorkflowGenerationId is not null)
        {
            var current = VerifyCurrent(location, project, package.Protocol);
            var currentPreview = PreviewMaterial(
                location, project, package.Protocol, current.Manifest.GenerationId,
                project.ReportingWorkflowManifestSha256!);
            return new PreparedPreview(location, project, package.Protocol, currentPreview);
        }
        var manifest = new ResearchWorkspaceReportingWorkflowManifest(
            ResearchWorkspaceReportingWorkflowManifest.CurrentSchema,
            generationId, project.WorkspaceId, checked(project.Revision + 1),
            workflow.Definition.WorkflowId, workflow.Definition.WorkflowDigest.ToString(),
            package.Protocol.Version.Id, package.Protocol.Version.ContentDigest.ToString(),
            workflow.ResolvedTemplate.TemplateId, workflow.ResolvedTemplate.TemplateVersion,
            workflow.ResolvedTemplate.TemplateDigest.ToString());
        var digest = ContentDigest.Sha256(Serialize(manifest)).ToString();
        return new PreparedPreview(
            location, project, package.Protocol,
            PreviewMaterial(location, project, package.Protocol, generationId, digest));
    }

    private static ResearchWorkspaceReportingWorkflowPreview PreviewMaterial(
        ResearchWorkspaceLocation location,
        ResearchWorkspaceProject project,
        VerifiedProtocolVersion protocol,
        string generationId,
        string manifestDigest)
    {
        var token = ContentDigest.Sha256CanonicalJson(new CanonicalJsonObject()
            .Add("schema", "nexus.workspace-reporting-workflow-preview")
            .Add("schema_version", "1.0.0")
            .Add("workspace_id", project.WorkspaceId)
            .Add("project_revision", project.Revision)
            .Add("protocol_content_digest", protocol.Version.ContentDigest.ToString())
            .Add("resulting_generation_id", generationId)
            .Add("resulting_manifest_digest", manifestDigest)
            .Add("effects", CanonicalJsonValue.Array(Effects.Select(CanonicalJsonValue.From).ToArray())))
            .ToString();
        return new ResearchWorkspaceReportingWorkflowPreview(
            ResearchWorkspaceOperationStatus.Succeeded,
            ResearchWorkspaceExitCodes.Success,
            "Review the reporting Workflow authority effects before confirmation.",
            location.RootDirectory, project.WorkspaceId, project.Revision,
            protocol.Version.ContentDigest.ToString(), generationId, manifestDigest,
            Effects, token);
    }

    private static ResearchWorkspaceReportingWorkflowCommitResult Failed(string message) =>
        new(ResearchWorkspaceOperationStatus.Failed,
            ResearchWorkspaceExitCodes.UsageOrValidationFailure,
            message, null, null, false);

    private static bool Same(
        ResearchWorkspaceReportingWorkflowPreview left,
        ResearchWorkspaceReportingWorkflowPreview right) =>
        left.WorkspaceDirectory == right.WorkspaceDirectory &&
        left.WorkspaceId == right.WorkspaceId &&
        left.ExpectedProjectRevision == right.ExpectedProjectRevision &&
        left.ProtocolContentDigest == right.ProtocolContentDigest &&
        left.ResultingGenerationId == right.ResultingGenerationId &&
        left.ResultingManifestDigest == right.ResultingManifestDigest &&
        left.ExpectedEffects.SequenceEqual(right.ExpectedEffects) &&
        left.ConfirmationToken == right.ConfirmationToken;

    private static byte[] Serialize(ResearchWorkspaceReportingWorkflowManifest value) =>
        CanonicalJsonSerializer.SerializeToUtf8Bytes(new CanonicalJsonObject()
            .Add("schema", value.Schema)
            .Add("generation_id", value.GenerationId)
            .Add("workspace_id", value.WorkspaceId)
            .Add("project_revision", value.ProjectRevision)
            .Add("workflow_id", value.WorkflowId)
            .Add("workflow_digest", value.WorkflowDigest)
            .Add("protocol_version_id", value.ProtocolVersionId)
            .Add("protocol_content_digest", value.ProtocolContentDigest)
            .Add("template_id", value.TemplateId)
            .Add("template_version", value.TemplateVersion)
            .Add("template_digest", value.TemplateDigest));

    private static ResearchWorkspaceReportingWorkflowManifest Rehydrate(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        var canonical = CanonicalJsonSerializer.SerializeToUtf8Bytes(
            CanonicalJsonValue.FromJsonElement(document.RootElement));
        if (!bytes.SequenceEqual(canonical))
            throw new InvalidOperationException("Reporting Workflow manifest is not canonical.");
        var root = document.RootElement;
        var expected = new[] { "generation_id", "project_revision", "protocol_content_digest",
            "protocol_version_id", "schema", "template_digest", "template_id", "template_version",
            "workflow_digest", "workflow_id", "workspace_id" };
        if (!root.EnumerateObject().Select(item => item.Name).OrderBy(item => item, StringComparer.Ordinal)
            .SequenceEqual(expected))
            throw new InvalidOperationException("Reporting Workflow manifest fields are invalid.");
        var result = new ResearchWorkspaceReportingWorkflowManifest(
            Text(root, "schema"), Text(root, "generation_id"), Text(root, "workspace_id"),
            root.GetProperty("project_revision").GetInt64(), Text(root, "workflow_id"),
            Text(root, "workflow_digest"), Text(root, "protocol_version_id"),
            Text(root, "protocol_content_digest"), Text(root, "template_id"),
            Text(root, "template_version"), Text(root, "template_digest"));
        if (result.Schema != ResearchWorkspaceReportingWorkflowManifest.CurrentSchema)
            throw new InvalidOperationException("Reporting Workflow manifest schema is invalid.");
        _ = ContentDigest.Parse(result.WorkflowDigest);
        _ = ContentDigest.Parse(result.ProtocolContentDigest);
        _ = ContentDigest.Parse(result.TemplateDigest);
        return result;
    }

    private static string Text(JsonElement root, string name) =>
        root.GetProperty(name).GetString() is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Reporting Workflow field '{name}' is required.");

    private static string Resolve(ResearchWorkspaceLocation location, string relativePath)
    {
        if (!ResearchWorkspaceVerifier.TryResolveWorkspaceRelativePath(
                location.RootDirectory, relativePath, out var path) || !File.Exists(path))
            throw new InvalidOperationException("Reporting Workflow manifest is missing or outside the workspace.");
        return path;
    }

    private sealed class Resolver(
        VerifiedProtocolVersion protocol,
        WorkflowTemplate template) : IWorkflowAuthorityResolver
    {
        public VerifiedProtocolVersion ResolveProtocolVersion(string protocolVersionId) =>
            protocol.Version.Id == protocolVersionId
                ? protocol
                : throw new InvalidOperationException("Unknown Protocol authority.");
        public VerifiedProtocolWaiver ResolveProtocolWaiver(string waiverId) =>
            throw new InvalidOperationException("The local reporting Workflow has no waiver bindings.");
        public VerifiedProtocolAmendment ResolveProtocolAmendment(string amendmentId) =>
            throw new InvalidOperationException("The local reporting Workflow has no amendment binding.");
        public WorkflowTemplate ResolveTemplate(
            string templateId, string templateVersion, ContentDigest expectedDigest) =>
            template.TemplateId == templateId && template.TemplateVersion == templateVersion &&
            template.TemplateDigest == expectedDigest
                ? template
                : throw new InvalidOperationException("Unknown reporting Workflow template authority.");
        public CanonicalJsonValue ResolveCompileParameter(
            string inputId, ContentDigest expectedValueDigest) =>
            throw new InvalidOperationException("The local reporting Workflow has no compile parameters.");
    }

    private sealed record PreparedPreview(
        ResearchWorkspaceLocation Location,
        ResearchWorkspaceProject Project,
        VerifiedProtocolVersion Protocol,
        ResearchWorkspaceReportingWorkflowPreview Preview);
}
