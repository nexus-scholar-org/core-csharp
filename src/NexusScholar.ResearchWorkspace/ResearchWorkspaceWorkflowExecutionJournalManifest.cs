using System.Globalization;
using System.Text.Json;
using NexusScholar.Kernel;

namespace NexusScholar.ResearchWorkspace;

public sealed record ResearchWorkspaceWorkflowExecutionJournalManifest(
    string Schema,
    string GenerationId,
    string WorkspaceId,
    long ProjectRevision,
    string ExecutionId,
    string WorkflowId,
    string WorkflowDigest,
    string ProtocolVersionId,
    string ProtocolContentDigest,
    string AuthorityPolicyId,
    string AuthorityPolicyDigest,
    string HeaderDigest,
    string? PredecessorGenerationId,
    string? PredecessorManifestSha256,
    string PriorHeadDigest,
    string ResultingHeadDigest,
    int EventCount,
    IReadOnlyList<ResearchWorkspaceGenerationArtifact> Artifacts)
{
    public const string CurrentSchema = "nexus.workspace-workflow-execution-journal-generation.v1";
}

public static class ResearchWorkspaceWorkflowExecutionJournalManifestCodec
{
    public static byte[] Serialize(ResearchWorkspaceWorkflowExecutionJournalManifest manifest)
    {
        Validate(manifest);
        var content = new CanonicalJsonObject()
            .Add("schema", manifest.Schema)
            .Add("generation_id", manifest.GenerationId)
            .Add("workspace_id", manifest.WorkspaceId)
            .Add("project_revision", manifest.ProjectRevision)
            .Add("execution_id", manifest.ExecutionId)
            .Add("workflow_id", manifest.WorkflowId)
            .Add("workflow_digest", manifest.WorkflowDigest)
            .Add("protocol_version_id", manifest.ProtocolVersionId)
            .Add("protocol_content_digest", manifest.ProtocolContentDigest)
            .Add("authority_policy_id", manifest.AuthorityPolicyId)
            .Add("authority_policy_digest", manifest.AuthorityPolicyDigest)
            .Add("header_digest", manifest.HeaderDigest)
            .Add("prior_head_digest", manifest.PriorHeadDigest)
            .Add("resulting_head_digest", manifest.ResultingHeadDigest)
            .Add("event_count", manifest.EventCount)
            .Add("artifacts", CanonicalJsonValue.Array(manifest.Artifacts
                .OrderBy(item => item.Name, StringComparer.Ordinal)
                .Select(Artifact).ToArray()));
        if (manifest.PredecessorGenerationId is not null)
        {
            content.Add("predecessor_generation_id", manifest.PredecessorGenerationId);
            content.Add("predecessor_manifest_sha256", manifest.PredecessorManifestSha256!);
        }
        return CanonicalJsonSerializer.SerializeToUtf8Bytes(content);
    }

    public static ResearchWorkspaceWorkflowExecutionJournalManifest Rehydrate(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        if (CanonicalJsonValue.FromJsonElement(document.RootElement) is not CanonicalJsonObject root ||
            !bytes.SequenceEqual(CanonicalJsonSerializer.SerializeToUtf8Bytes(root)))
            throw new InvalidOperationException("Workflow execution journal manifest must use canonical JSON bytes.");
        var required = new[]
        {
            "artifacts", "authority_policy_digest", "authority_policy_id", "event_count", "execution_id",
            "generation_id", "header_digest", "prior_head_digest", "project_revision", "protocol_content_digest",
            "protocol_version_id", "resulting_head_digest", "schema", "workflow_digest", "workflow_id", "workspace_id"
        };
        var allowed = required.Concat(new[] { "predecessor_generation_id", "predecessor_manifest_sha256" }).ToHashSet(StringComparer.Ordinal);
        if (!required.All(root.Properties.ContainsKey) || root.Properties.Keys.Any(key => !allowed.Contains(key)))
            throw new InvalidOperationException("Workflow execution journal manifest has missing or unknown fields.");

        var manifest = new ResearchWorkspaceWorkflowExecutionJournalManifest(
            Text(root, "schema"), Text(root, "generation_id"), Text(root, "workspace_id"), Long(root, "project_revision"),
            Text(root, "execution_id"), Text(root, "workflow_id"), Text(root, "workflow_digest"),
            Text(root, "protocol_version_id"), Text(root, "protocol_content_digest"), Text(root, "authority_policy_id"),
            Text(root, "authority_policy_digest"), Text(root, "header_digest"), OptionalText(root, "predecessor_generation_id"),
            OptionalText(root, "predecessor_manifest_sha256"), Text(root, "prior_head_digest"), Text(root, "resulting_head_digest"),
            checked((int)Long(root, "event_count")), Array(root, "artifacts").Select(ParseArtifact).ToArray());
        Validate(manifest);
        return manifest;
    }

    private static CanonicalJsonObject Artifact(ResearchWorkspaceGenerationArtifact artifact) => new CanonicalJsonObject()
        .Add("name", artifact.Name)
        .Add("relative_path", artifact.RelativePath)
        .Add("sha256", artifact.Sha256);

    private static ResearchWorkspaceGenerationArtifact ParseArtifact(CanonicalJsonValue value)
    {
        if (value is not CanonicalJsonObject obj || obj.Properties.Count != 3)
            throw new InvalidOperationException("Workflow execution journal artifact entry is invalid.");
        return new ResearchWorkspaceGenerationArtifact(Text(obj, "name"), Text(obj, "relative_path"), Text(obj, "sha256"));
    }

    private static void Validate(ResearchWorkspaceWorkflowExecutionJournalManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (manifest.Schema != ResearchWorkspaceWorkflowExecutionJournalManifest.CurrentSchema || manifest.ProjectRevision <= 0 ||
            manifest.EventCount < 0 || manifest.Artifacts.Count != manifest.EventCount + 2 ||
            manifest.Artifacts.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != manifest.Artifacts.Count ||
            (manifest.PredecessorGenerationId is null) != (manifest.PredecessorManifestSha256 is null))
            throw new InvalidOperationException("Workflow execution journal manifest shape is invalid.");
        foreach (var digest in new[]
        {
            manifest.WorkflowDigest, manifest.ProtocolContentDigest, manifest.AuthorityPolicyDigest, manifest.HeaderDigest,
            manifest.PriorHeadDigest, manifest.ResultingHeadDigest
        }.Concat(manifest.PredecessorManifestSha256 is null ? System.Array.Empty<string>() : new[] { manifest.PredecessorManifestSha256 }))
            _ = ContentDigest.Parse(digest);
        foreach (var artifact in manifest.Artifacts) _ = ContentDigest.Parse(artifact.Sha256);
    }

    private static string Text(CanonicalJsonObject root, string name) => root.Properties.TryGetValue(name, out var value) && value is CanonicalJsonString text
        ? text.Value : throw new InvalidOperationException($"Manifest field '{name}' must be a string.");

    private static string? OptionalText(CanonicalJsonObject root, string name) => root.Properties.ContainsKey(name) ? Text(root, name) : null;

    private static long Long(CanonicalJsonObject root, string name) => root.Properties.TryGetValue(name, out var value) &&
        value is CanonicalJsonNumber number && long.TryParse(number.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var result)
            ? result : throw new InvalidOperationException($"Manifest field '{name}' must be an integer.");

    private static IReadOnlyList<CanonicalJsonValue> Array(CanonicalJsonObject root, string name) =>
        root.Properties.TryGetValue(name, out var value) && value is CanonicalJsonArray array
            ? array.Items : throw new InvalidOperationException($"Manifest field '{name}' must be an array.");
}
