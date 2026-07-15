using System.Text.Json;
using NexusScholar.Kernel;

namespace NexusScholar.ResearchWorkspace;

public static class ResearchWorkspaceSuccessorAuthorityManifestCodec
{
    public static readonly IReadOnlyList<string> ArtifactNames = Array.AsReadOnly(new[]
    {
        "authority-policy",
        "decision",
        "decision-recorded-event",
        "invalidation",
        "review-command",
        "snapshot-invalidated-event",
        "snapshot-publication-event",
        "successor-snapshot"
    });

    public static byte[] Serialize(ResearchWorkspaceSuccessorAuthorityGenerationManifest manifest)
    {
        Validate(manifest);
        return CanonicalJsonSerializer.SerializeToUtf8Bytes(BuildCanonical(manifest));
    }

    public static ResearchWorkspaceSuccessorAuthorityGenerationManifest ParseCanonical(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var canonical = CanonicalJsonSerializer.SerializeToUtf8Bytes(CanonicalJsonValue.FromJsonElement(document.RootElement));
        if (!bytes.SequenceEqual(canonical))
        {
            throw new InvalidOperationException("Successor authority manifest is not canonical.");
        }

        var root = document.RootElement;
        var artifacts = root.GetProperty("artifacts").EnumerateArray()
            .Select(item => new ResearchWorkspaceGenerationArtifact(
                item.GetProperty("name").GetString()!,
                item.GetProperty("relative_path").GetString()!,
                item.GetProperty("sha256").GetString()!))
            .ToArray();
        var manifest = new ResearchWorkspaceSuccessorAuthorityGenerationManifest(
            Require(root, "schema"),
            Require(root, "authority_generation_id"),
            Require(root, "workspace_id"),
            root.GetProperty("project_revision").GetInt64(),
            Require(root, "transition_kind"),
            Require(root, "source_analysis_generation_id"),
            Require(root, "source_analysis_manifest_sha256"),
            Require(root, "source_result_id"),
            Require(root, "source_result_digest"),
            Require(root, "predecessor_authority_generation_id"),
            Require(root, "predecessor_authority_generation_manifest_sha256"),
            Require(root, "request_id"),
            Require(root, "request_digest"),
            Require(root, "authority_policy_id"),
            Require(root, "authority_policy_digest"),
            Require(root, "decision_id"),
            Require(root, "decision_digest"),
            Require(root, "predecessor_snapshot_id"),
            Require(root, "predecessor_snapshot_record_digest"),
            Require(root, "successor_snapshot_id"),
            Require(root, "successor_snapshot_content_digest"),
            Require(root, "successor_snapshot_record_digest"),
            Require(root, "invalidation_id"),
            Require(root, "invalidation_record_digest"),
            Require(root, "decision_set_digest"),
            Require(root, "decision_recorded_event_digest"),
            Require(root, "snapshot_invalidated_event_digest"),
            Require(root, "snapshot_publication_event_digest"),
            artifacts);
        Validate(manifest);
        if (!bytes.SequenceEqual(Serialize(manifest)))
        {
            throw new InvalidOperationException("Successor authority manifest contains unsupported or non-canonical material.");
        }

        return manifest;
    }

    private static CanonicalJsonObject BuildCanonical(ResearchWorkspaceSuccessorAuthorityGenerationManifest value) =>
        new CanonicalJsonObject()
            .Add("schema", value.Schema)
            .Add("authority_generation_id", value.AuthorityGenerationId)
            .Add("workspace_id", value.WorkspaceId)
            .Add("project_revision", value.ProjectRevision)
            .Add("transition_kind", value.TransitionKind)
            .Add("source_analysis_generation_id", value.SourceAnalysisGenerationId)
            .Add("source_analysis_manifest_sha256", value.SourceAnalysisManifestSha256)
            .Add("source_result_id", value.SourceResultId)
            .Add("source_result_digest", value.SourceResultDigest)
            .Add("predecessor_authority_generation_id", value.PredecessorAuthorityGenerationId)
            .Add("predecessor_authority_generation_manifest_sha256", value.PredecessorAuthorityGenerationManifestSha256)
            .Add("request_id", value.RequestId)
            .Add("request_digest", value.RequestDigest)
            .Add("authority_policy_id", value.AuthorityPolicyId)
            .Add("authority_policy_digest", value.AuthorityPolicyDigest)
            .Add("decision_id", value.DecisionId)
            .Add("decision_digest", value.DecisionDigest)
            .Add("predecessor_snapshot_id", value.PredecessorSnapshotId)
            .Add("predecessor_snapshot_record_digest", value.PredecessorSnapshotRecordDigest)
            .Add("successor_snapshot_id", value.SuccessorSnapshotId)
            .Add("successor_snapshot_content_digest", value.SuccessorSnapshotContentDigest)
            .Add("successor_snapshot_record_digest", value.SuccessorSnapshotRecordDigest)
            .Add("invalidation_id", value.InvalidationId)
            .Add("invalidation_record_digest", value.InvalidationRecordDigest)
            .Add("decision_set_digest", value.DecisionSetDigest)
            .Add("decision_recorded_event_digest", value.DecisionRecordedEventDigest)
            .Add("snapshot_invalidated_event_digest", value.SnapshotInvalidatedEventDigest)
            .Add("snapshot_publication_event_digest", value.SnapshotPublicationEventDigest)
            .Add("artifacts", CanonicalJsonValue.Array(value.Artifacts.Select(item =>
                (CanonicalJsonValue)new CanonicalJsonObject()
                    .Add("name", item.Name)
                    .Add("relative_path", item.RelativePath)
                    .Add("sha256", item.Sha256)).ToArray()));

    private static void Validate(ResearchWorkspaceSuccessorAuthorityGenerationManifest value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!string.Equals(value.Schema, ResearchWorkspaceSuccessorAuthorityGenerationManifest.CurrentSchema, StringComparison.Ordinal) ||
            !string.Equals(value.TransitionKind, ResearchWorkspaceSuccessorAuthorityGenerationManifest.DeduplicationDecisionTransition, StringComparison.Ordinal) ||
            value.ProjectRevision < 1)
        {
            throw new InvalidOperationException("Successor authority manifest schema, transition, or revision is invalid.");
        }

        var required = new[]
        {
            value.AuthorityGenerationId, value.WorkspaceId, value.SourceAnalysisGenerationId,
            value.SourceResultId, value.PredecessorAuthorityGenerationId, value.RequestId,
            value.AuthorityPolicyId, value.DecisionId, value.PredecessorSnapshotId,
            value.SuccessorSnapshotId, value.InvalidationId
        };
        if (required.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Successor authority manifest identifiers are required.");
        }

        var digests = new[]
        {
            value.SourceAnalysisManifestSha256, value.SourceResultDigest,
            value.PredecessorAuthorityGenerationManifestSha256, value.RequestDigest,
            value.AuthorityPolicyDigest, value.DecisionDigest,
            value.PredecessorSnapshotRecordDigest, value.SuccessorSnapshotContentDigest,
            value.SuccessorSnapshotRecordDigest, value.InvalidationRecordDigest,
            value.DecisionSetDigest, value.DecisionRecordedEventDigest,
            value.SnapshotInvalidatedEventDigest, value.SnapshotPublicationEventDigest
        };
        if (digests.Any(item => !ContentDigest.TryParse(item, out _)))
        {
            throw new InvalidOperationException("Successor authority manifest contains an invalid digest.");
        }

        if (value.Artifacts is null || !value.Artifacts.Select(item => item.Name).SequenceEqual(ArtifactNames, StringComparer.Ordinal) ||
            value.Artifacts.Any(item => string.IsNullOrWhiteSpace(item.RelativePath) || !ContentDigest.TryParse(item.Sha256, out _)))
        {
            throw new InvalidOperationException("Successor authority manifest artifact set is incomplete or non-canonical.");
        }

        var generationPrefix = ResearchWorkspacePaths.AuthorityGenerationRoot(value.AuthorityGenerationId) + "/";
        foreach (var artifact in value.Artifacts)
        {
            var expectedPath = generationPrefix + (artifact.Name switch
            {
                "authority-policy" => "authority-policy.json",
                "decision" => "decision.json",
                "decision-recorded-event" => "decision-recorded-event.json",
                "invalidation" => "invalidation.json",
                "review-command" => "review-command.json",
                "snapshot-invalidated-event" => "snapshot-invalidated-event.json",
                "snapshot-publication-event" => "snapshot-publication-event.json",
                "successor-snapshot" => "successor-snapshot.json",
                _ => throw new InvalidOperationException("Successor authority artifact name is unsupported.")
            });
            if (!string.Equals(artifact.RelativePath, expectedPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Successor authority artifact path is not canonical for its generation.");
            }
        }
    }

    private static string Require(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidOperationException($"Successor authority manifest field '{name}' is required.");
        }

        return value.GetString()!;
    }
}
