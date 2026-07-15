using System.Text.Json;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;
using NexusScholar.Provenance;

namespace NexusScholar.ResearchWorkspace;

public static class ResearchWorkspaceAuthorityGenerationVerifier
{
    public static ResearchWorkspaceVerifiedAuthorityGeneration? VerifyCurrent(
        ResearchWorkspaceLocation location,
        ResearchWorkspaceProject project,
        VerifiedDeduplicationAuthorityResultDigest sourceResult)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(sourceResult);
        if (project.CurrentAuthorityGenerationId is null)
        {
            return null;
        }

        var manifestPath = Resolve(location, project.AuthorityGenerationManifestPath!);
        var manifestBytes = File.ReadAllBytes(manifestPath);
        if (ContentDigest.Sha256(manifestBytes).ToString() != project.AuthorityGenerationManifestSha256)
        {
            throw new InvalidOperationException("Authority generation manifest failed project-pointer digest verification.");
        }

        using var document = JsonDocument.Parse(manifestBytes);
        var canonicalBytes = CanonicalJsonSerializer.SerializeToUtf8Bytes(CanonicalJsonValue.FromJsonElement(document.RootElement));
        if (!manifestBytes.SequenceEqual(canonicalBytes))
        {
            throw new InvalidOperationException("Authority generation manifest is not canonical.");
        }

        var root = document.RootElement;
        if (string.Equals(root.GetProperty("schema").GetString(),
            ResearchWorkspaceSuccessorAuthorityGenerationManifest.CurrentSchema, StringComparison.Ordinal))
        {
            var chain = ResearchWorkspaceAuthorityChainVerifier.VerifyCurrent(location, project, sourceResult);
            return new ResearchWorkspaceVerifiedAuthorityGeneration(
                chain.Policy, chain.CurrentSnapshot, chain.CurrentPublicationEvent);
        }

        Require(root, "schema", ResearchWorkspaceAuthorityGenerationManifest.CurrentSchema);
        Require(root, "authority_generation_id", project.CurrentAuthorityGenerationId);
        Require(root, "workspace_id", project.WorkspaceId);
        if (root.GetProperty("project_revision").GetInt64() != project.Revision ||
            !string.Equals(root.GetProperty("source_result_id").GetString(), sourceResult.Result.ResultId, StringComparison.Ordinal) ||
            !string.Equals(root.GetProperty("source_result_digest").GetString(), sourceResult.ResultDigest.ToString(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Authority generation manifest is stale or bound to another source result.");
        }

        Require(root, "source_analysis_generation_id", project.CurrentGenerationId!);
        var analysisManifestPath = Resolve(location, project.GenerationManifestPath!);
        var analysisManifestDigest = ContentDigest.Sha256(File.ReadAllBytes(analysisManifestPath));
        Require(root, "source_analysis_manifest_sha256", analysisManifestDigest.ToString());

        if (root.GetProperty("predecessor_authority_generation_id").ValueKind != JsonValueKind.Null ||
            root.GetProperty("predecessor_authority_generation_manifest_sha256").ValueKind != JsonValueKind.Null)
        {
            throw new InvalidOperationException("FE-01 authority initialization cannot have a predecessor generation.");
        }

        var artifacts = root.GetProperty("artifacts").EnumerateArray().ToArray();
        var expectedNames = new[] { "authority-policy", "baseline-snapshot", "snapshot-publication-event" };
        if (artifacts.Length != expectedNames.Length)
        {
            throw new InvalidOperationException("Authority generation must contain exactly three manifest artifacts.");
        }

        var bytesByName = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        for (var index = 0; index < artifacts.Length; index++)
        {
            var artifact = artifacts[index];
            var name = artifact.GetProperty("name").GetString()!;
            if (!string.Equals(name, expectedNames[index], StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Authority artifacts are not in canonical name order.");
            }

            var relativePath = artifact.GetProperty("relative_path").GetString()!;
            var bytes = File.ReadAllBytes(Resolve(location, relativePath));
            if (!string.Equals(ContentDigest.Sha256(bytes).ToString(), artifact.GetProperty("sha256").GetString(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Authority artifact '{name}' failed raw digest verification.");
            }

            bytesByName.Add(name, bytes);
        }

        var policy = ResearchWorkspaceAuthorityArtifacts.VerifyPolicyCanonicalRecord(bytesByName["authority-policy"]);
        var snapshot = ResearchWorkspaceAuthorityArtifacts.VerifySnapshotCanonicalRecord(bytesByName["baseline-snapshot"], sourceResult, policy);
        var publicationEvent = ResearchWorkspaceAuthorityArtifacts.VerifyResearchEventCanonicalRecord(bytesByName["snapshot-publication-event"]);
        if (!string.Equals(root.GetProperty("authority_policy_id").GetString(), policy.PolicyId, StringComparison.Ordinal) ||
            !string.Equals(root.GetProperty("authority_policy_digest").GetString(), policy.PolicyDigest.ToString(), StringComparison.Ordinal) ||
            !string.Equals(root.GetProperty("decision_set_digest").GetString(), snapshot.DecisionSetDigest.ToString(), StringComparison.Ordinal) ||
            !HasExactPublicationBinding(publicationEvent, sourceResult, policy, snapshot, project.CurrentGenerationId!, analysisManifestDigest))
        {
            throw new InvalidOperationException("Authority manifest and verified domain records do not agree.");
        }

        return new ResearchWorkspaceVerifiedAuthorityGeneration(policy, snapshot, publicationEvent);
    }

    private static bool HasExactPublicationBinding(
        ResearchEvent publicationEvent,
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedDeduplicationAuthorityPolicy policy,
        VerifiedCorpusSnapshot snapshot,
        string analysisGenerationId,
        ContentDigest analysisManifestDigest)
    {
        var expectedSnapshot = new ProvenanceEntityRef("nexus.corpus.snapshot", snapshot.SnapshotId, snapshot.RecordDigest);
        var expectedInputs = new[]
        {
            new ProvenanceEntityRef("nexus.deduplication.result", sourceResult.Result.ResultId, sourceResult.ResultDigest),
            new ProvenanceEntityRef(DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind, policy.PolicyId, policy.PolicyDigest),
            new ProvenanceEntityRef("source-analysis-manifest", analysisGenerationId, analysisManifestDigest),
            new ProvenanceEntityRef("deduplication-decision-set", "decision-set-empty", snapshot.DecisionSetDigest)
        };
        return string.Equals(publicationEvent.Activity.ActivityId, "corpus-snapshot-published", StringComparison.Ordinal) &&
            publicationEvent.Agent.AgentKind == ProvenanceAgent.HumanKind &&
            string.Equals(publicationEvent.Agent.AgentId, snapshot.CreatedByActorId, StringComparison.Ordinal) &&
            publicationEvent.Subject == expectedSnapshot &&
            publicationEvent.Outputs.Count == 1 && publicationEvent.Outputs[0] == expectedSnapshot &&
            publicationEvent.Inputs.SequenceEqual(expectedInputs) &&
            publicationEvent.ProtocolBinding is null && publicationEvent.WorkflowBinding is null;
    }

    private static string Resolve(ResearchWorkspaceLocation location, string relativePath)
    {
        if (!ResearchWorkspaceVerifier.TryResolveWorkspaceRelativePath(location.RootDirectory, relativePath, out var path) || !File.Exists(path))
        {
            throw new InvalidOperationException("Authority generation file is missing or outside the workspace.");
        }

        return path;
    }

    private static void Require(JsonElement root, string propertyName, string expected)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String ||
            !string.Equals(value.GetString(), expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Authority manifest field '{propertyName}' is invalid.");
        }
    }
}

public sealed record ResearchWorkspaceVerifiedAuthorityGeneration(
    VerifiedDeduplicationAuthorityPolicy Policy,
    VerifiedCorpusSnapshot Snapshot,
    ResearchEvent PublicationEvent);
