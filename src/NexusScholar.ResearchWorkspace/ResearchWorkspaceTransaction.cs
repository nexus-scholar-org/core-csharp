using System.Text.Json;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;
using NexusScholar.Provenance;
using NexusScholar.Search;
using NexusScholar.UiContracts;

namespace NexusScholar.ResearchWorkspace;

public static class ResearchWorkspaceTransaction
{
    public static ResearchWorkspaceAuthorityCommit InitializeAuthorityGeneration(
        ResearchWorkspaceLocation location,
        ResearchWorkspaceProject expectedProject,
        string expectedAnalysisGenerationId,
        string expectedAnalysisManifestSha256,
        string snapshotId,
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedDeduplicationAuthorityPolicy policy,
        string publisherActorId,
        string publisherRole,
        IClock clock,
        IIdGenerator idGenerator,
        Action<ResearchWorkspaceAuthorityFaultPoint>? faultInjector = null)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(expectedProject);
        ArgumentNullException.ThrowIfNull(sourceResult);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(idGenerator);
        RejectActiveAuthority(expectedProject);

        if (!string.Equals(expectedProject.CurrentGenerationId, expectedAnalysisGenerationId, StringComparison.Ordinal) ||
            expectedProject.GenerationManifestPath is null ||
            !ContentDigest.TryParse(expectedAnalysisManifestSha256, out var expectedManifestDigest))
        {
            throw new ResearchWorkspaceConcurrencyException("The expected analysis generation binding is stale or malformed.", new InvalidOperationException());
        }

        var sourceManifestPath = ResolveRequiredPath(location, expectedProject.GenerationManifestPath);
        if (ContentDigest.Sha256(File.ReadAllBytes(sourceManifestPath)) != expectedManifestDigest)
        {
            throw new ResearchWorkspaceConcurrencyException("The expected analysis manifest digest is stale.", new InvalidOperationException());
        }
        VerifySourceResultBinding(location, expectedProject, sourceResult);

        var baseline = CorpusSnapshotService.CreateBaseline(
            snapshotId,
            sourceResult,
            policy,
            publisherActorId,
            publisherRole,
            clock);
        var publishedEvent = BuildBaselinePublicationEvent(
            expectedProject,
            expectedAnalysisGenerationId,
            expectedManifestDigest,
            sourceResult,
            policy,
            baseline,
            publisherActorId,
            clock,
            idGenerator);

        var authorityGenerationId = $"authority-{Guid.NewGuid():N}";
        var generationRelative = ResearchWorkspacePaths.AuthorityGenerationRoot(authorityGenerationId);
        var stagingRoot = ResearchWorkspacePaths.InProject(
            location.RootDirectory,
            $"{ResearchWorkspacePaths.GenerationStaging}/{authorityGenerationId}");
        var generationRoot = ResearchWorkspacePaths.InProject(location.RootDirectory, generationRelative);
        Directory.CreateDirectory(stagingRoot);

        try
        {
            var policyBytes = ResearchWorkspaceAuthorityArtifacts.SerializePolicyCanonicalRecord(policy);
            var snapshotBytes = ResearchWorkspaceAuthorityArtifacts.SerializeSnapshotCanonicalRecord(baseline);
            var eventBytes = ResearchWorkspaceAuthorityArtifacts.SerializeResearchEventCanonicalRecord(publishedEvent);
            var artifacts = new[]
            {
                WriteCanonicalArtifact("authority-policy", "authority-policy.json", policyBytes),
                WriteCanonicalArtifact("baseline-snapshot", "baseline-snapshot.json", snapshotBytes),
                WriteCanonicalArtifact("snapshot-publication-event", "snapshot-publication-event.json", eventBytes)
            }.OrderBy(item => item.Name, StringComparer.Ordinal).ToArray();

            _ = ResearchWorkspaceAuthorityArtifacts.VerifyPolicyCanonicalRecord(policyBytes);
            _ = ResearchWorkspaceAuthorityArtifacts.VerifySnapshotCanonicalRecord(snapshotBytes, sourceResult, policy);
            _ = ResearchWorkspaceAuthorityArtifacts.VerifyResearchEventCanonicalRecord(eventBytes);

            var manifestPath = $"{generationRelative}/authority-generation.manifest.json";
            var committedProject = expectedProject.CommitAuthorityGeneration(
                authorityGenerationId,
                manifestPath,
                "sha256:" + new string('0', 64));
            var manifest = new ResearchWorkspaceAuthorityGenerationManifest(
                ResearchWorkspaceAuthorityGenerationManifest.CurrentSchema,
                authorityGenerationId,
                expectedProject.WorkspaceId,
                committedProject.Revision,
                expectedAnalysisGenerationId,
                expectedManifestDigest.ToString(),
                sourceResult.Result.ResultId,
                sourceResult.ResultDigest.ToString(),
                null,
                null,
                policy.PolicyId,
                policy.PolicyDigest.ToString(),
                baseline.DecisionSetDigest.ToString(),
                artifacts);
            var manifestBytes = SerializeAuthorityManifest(manifest);
            var manifestDigest = ContentDigest.Sha256(manifestBytes);
            committedProject = committedProject with { AuthorityGenerationManifestSha256 = manifestDigest.ToString() };
            File.WriteAllBytes(Path.Combine(stagingRoot, "authority-generation.manifest.json"), manifestBytes);
            VerifyStagedAuthorityManifest(stagingRoot, generationRelative, manifestBytes, manifest);
            faultInjector?.Invoke(ResearchWorkspaceAuthorityFaultPoint.AfterStaging);

            Directory.CreateDirectory(Path.GetDirectoryName(generationRoot)!);
            using var workspaceLock = AcquireLock(location);
            var currentProject = ResearchWorkspaceStore.ReadProject(location.ProjectFilePath);
            RecoverOrphanedAuthorityGenerations(location, currentProject);
            RejectActiveAuthority(currentProject);
            if (currentProject.Revision != expectedProject.Revision ||
                !string.Equals(currentProject.WorkspaceId, expectedProject.WorkspaceId, StringComparison.Ordinal) ||
                !string.Equals(currentProject.CurrentGenerationId, expectedAnalysisGenerationId, StringComparison.Ordinal) ||
                !string.Equals(currentProject.GenerationManifestPath, expectedProject.GenerationManifestPath, StringComparison.Ordinal))
            {
                throw new ResearchWorkspaceConcurrencyException(expectedProject.Revision, currentProject.Revision);
            }

            var lockedManifestPath = ResolveRequiredPath(location, currentProject.GenerationManifestPath!);
            if (ContentDigest.Sha256(File.ReadAllBytes(lockedManifestPath)) != expectedManifestDigest)
            {
                throw new ResearchWorkspaceConcurrencyException("The source analysis manifest changed during authority initialization.", new InvalidOperationException());
            }
            VerifySourceResultBinding(location, currentProject, sourceResult);

            Directory.Move(stagingRoot, generationRoot);
            try
            {
                faultInjector?.Invoke(ResearchWorkspaceAuthorityFaultPoint.AfterPromotion);
                ResearchWorkspaceStore.WriteProject(location, committedProject);
            }
            catch
            {
                Quarantine(location, generationRoot, authorityGenerationId);
                throw;
            }

            return new ResearchWorkspaceAuthorityCommit(committedProject, manifest, baseline, publishedEvent);

            ResearchWorkspaceGenerationArtifact WriteCanonicalArtifact(string name, string fileName, byte[] bytes)
            {
                var path = Path.Combine(stagingRoot, fileName);
                File.WriteAllBytes(path, bytes);
                return new ResearchWorkspaceGenerationArtifact(
                    name,
                    $"{generationRelative}/{fileName}",
                    ContentDigest.Sha256(bytes).ToString());
            }
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, recursive: true);
            }
        }
    }

    public static ResearchWorkspaceProject CommitImport(
        ResearchWorkspaceLocation location,
        ResearchWorkspaceProject expectedProject,
        ResearchWorkspaceInput input,
        byte[] sourceBytes,
        SearchImportTrace trace,
        string sourceExtension)
    {
        RejectActiveAuthority(expectedProject);
        var importId = $"import-{Guid.NewGuid():N}";
        var stagingRoot = ResearchWorkspacePaths.InProject(location.RootDirectory, $"{ResearchWorkspacePaths.GenerationStaging}/{importId}");
        var importRelative = $"{ResearchWorkspacePaths.SearchInputs}/{input.EffectiveInputId}";
        var importRoot = ResearchWorkspacePaths.InProject(location.RootDirectory, importRelative);
        var sourceRelative = $"{importRelative}/source.{sourceExtension}";
        var traceRelative = $"{importRelative}/import-trace.json";
        Directory.CreateDirectory(stagingRoot);
        try
        {
            File.WriteAllBytes(Path.Combine(stagingRoot, $"source.{sourceExtension}"), sourceBytes);
            ResearchWorkspaceJson.WriteJsonFile(Path.Combine(stagingRoot, "import-trace.json"), trace);
            var committedInput = input with { RelativePath = sourceRelative, ImportTracePath = traceRelative };
            var committedProject = expectedProject.WithInput(committedInput) with
            {
                Revision = checked(expectedProject.Revision + 1),
                Outputs = new Dictionary<string, string>(StringComparer.Ordinal),
                CurrentGenerationId = null,
                GenerationManifestPath = null
            };

            Directory.CreateDirectory(Path.GetDirectoryName(importRoot)!);
            using var workspaceLock = AcquireLock(location);
            var currentProject = ResearchWorkspaceStore.ReadProject(location.ProjectFilePath);
            RejectActiveAuthority(currentProject);
            if (currentProject.Revision != expectedProject.Revision)
            {
                throw new ResearchWorkspaceConcurrencyException(expectedProject.Revision, currentProject.Revision);
            }

            Directory.Move(stagingRoot, importRoot);
            try
            {
                ResearchWorkspaceStore.WriteProject(location, committedProject);
            }
            catch
            {
                Quarantine(location, importRoot, importId);
                throw;
            }

            return committedProject;
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, recursive: true);
            }
        }
    }

    public static ResearchWorkspaceAnalysisCommit AnalyzeAndCommit(
        ResearchWorkspaceLocation location,
        ResearchWorkspaceProject expectedProject)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(expectedProject);
        RejectActiveAuthority(expectedProject);

        var analysis = ResearchWorkspaceAnalyzer.Analyze(location, expectedProject);
        var generationId = $"gen-{Guid.NewGuid():N}";
        var stagingRelative = $"{ResearchWorkspacePaths.GenerationStaging}/{generationId}";
        var stagingRoot = ResearchWorkspacePaths.InProject(location.RootDirectory, stagingRelative);
        var generationRelative = ResearchWorkspacePaths.GenerationRoot(generationId);
        var generationRoot = ResearchWorkspacePaths.InProject(location.RootDirectory, generationRelative);
        Directory.CreateDirectory(stagingRoot);

        try
        {
            var traceArtifacts = WriteTraces(stagingRoot, generationRelative, analysis);
            var outputArtifacts = WriteOutputs(stagingRoot, generationRelative, analysis);
            var updatedInputs = expectedProject.Inputs.Select(input =>
            {
                var trace = traceArtifacts.Single(artifact => string.Equals(artifact.Name, input.EffectiveInputId, StringComparison.Ordinal));
                return input with { ImportTracePath = trace.RelativePath };
            }).ToArray();
            var outputs = outputArtifacts.ToDictionary(artifact => artifact.Name, artifact => artifact.RelativePath, StringComparer.Ordinal);
            var manifestPath = $"{generationRelative}/generation.manifest.json";
            var committedProject = (expectedProject with { Inputs = updatedInputs }).CommitGeneration(outputs, generationId, manifestPath);
            var manifest = new ResearchWorkspaceGenerationManifest(
                ResearchWorkspaceGenerationManifest.CurrentSchema,
                generationId,
                expectedProject.WorkspaceId,
                committedProject.Revision,
                expectedProject.Inputs.OrderBy(input => input.EffectiveInputId, StringComparer.Ordinal)
                    .Select(input => new ResearchWorkspaceGenerationArtifact(input.EffectiveInputId, input.EffectiveRelativePath, input.Sha256)).ToArray(),
                traceArtifacts,
                outputArtifacts);
            ResearchWorkspaceJson.WriteJsonFile(Path.Combine(stagingRoot, "generation.manifest.json"), manifest);

            Directory.CreateDirectory(Path.GetDirectoryName(generationRoot)!);
            using var workspaceLock = AcquireLock(location);
            var currentProject = ResearchWorkspaceStore.ReadProject(location.ProjectFilePath);
            RejectActiveAuthority(currentProject);
            if (currentProject.Revision != expectedProject.Revision ||
                !string.Equals(currentProject.WorkspaceId, expectedProject.WorkspaceId, StringComparison.Ordinal))
            {
                throw new ResearchWorkspaceConcurrencyException(expectedProject.Revision, currentProject.Revision);
            }

            Directory.Move(stagingRoot, generationRoot);
            try
            {
                ResearchWorkspaceStore.WriteProject(location, committedProject);
            }
            catch
            {
                Quarantine(location, generationRoot, generationId);
                throw;
            }

            return new ResearchWorkspaceAnalysisCommit(analysis, committedProject, manifest);
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, recursive: true);
            }
        }
    }

    private static IReadOnlyList<ResearchWorkspaceGenerationArtifact> WriteTraces(
        string stagingRoot,
        string generationRelative,
        ResearchWorkspaceAnalysisResult analysis)
    {
        var artifacts = new List<ResearchWorkspaceGenerationArtifact>();
        foreach (var trace in analysis.ImportTraces.OrderBy(trace => trace.TraceId, StringComparer.Ordinal))
        {
            var inputId = trace.TraceId.EndsWith(".import-trace", StringComparison.Ordinal)
                ? trace.TraceId[..^".import-trace".Length]
                : trace.TraceId;
            var local = $"imports/{inputId}.import-trace.json";
            var path = Path.Combine(stagingRoot, local.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            ResearchWorkspaceJson.WriteJsonFile(path, trace);
            artifacts.Add(Artifact(inputId, $"{generationRelative}/{local}", path));
        }

        return artifacts;
    }

    private static IReadOnlyList<ResearchWorkspaceGenerationArtifact> WriteOutputs(
        string stagingRoot,
        string generationRelative,
        ResearchWorkspaceAnalysisResult analysis)
    {
        var files = new[]
        {
            Write("deduplicationResult", "dedup/current.deduplication-result.json", value => ResearchWorkspaceJson.WriteJsonFile(value, analysis.DeduplicationResult)),
            Write("workspacePlan", "workspace/current.workspace-plan.json", value => ResearchWorkspaceJson.WriteJsonFile(value, analysis.WorkspacePlan, UiContractJson.SerializerOptions)),
            Write("reviewReport", "reports/current.review-report.md", value => ResearchWorkspaceJson.WriteTextFile(value, WorkspacePlanReportWriter.Format(analysis)))
        };
        return files;

        ResearchWorkspaceGenerationArtifact Write(string name, string local, Action<string> writer)
        {
            var path = Path.Combine(stagingRoot, local.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            writer(path);
            return Artifact(name, $"{generationRelative}/{local}", path);
        }
    }

    private static ResearchWorkspaceGenerationArtifact Artifact(string name, string relativePath, string path) =>
        new(name, relativePath, ContentDigest.Sha256(File.ReadAllBytes(path)).ToString());

    private static string ResolveRequiredPath(ResearchWorkspaceLocation location, string relativePath)
    {
        if (!ResearchWorkspaceVerifier.TryResolveWorkspaceRelativePath(location.RootDirectory, relativePath, out var path) || !File.Exists(path))
        {
            throw new ResearchWorkspaceMissingInputException("The source analysis manifest is missing or outside the workspace.");
        }

        return path;
    }

    private static void VerifySourceResultBinding(
        ResearchWorkspaceLocation location,
        ResearchWorkspaceProject project,
        VerifiedDeduplicationAuthorityResultDigest expectedSourceResult)
    {
        var generation = ResearchWorkspaceGenerationVerifier.VerifyCurrent(location, project)
            ?? throw new ResearchWorkspaceMissingInputException("A committed analysis generation is required before authority initialization.");
        var resultArtifact = generation.Outputs.SingleOrDefault(item => string.Equals(item.Name, "deduplicationResult", StringComparison.Ordinal))
            ?? throw new ResearchWorkspaceMissingInputException("The analysis generation does not contain a deduplication result.");
        var resultPath = ResolveRequiredPath(location, resultArtifact.RelativePath);
        var persisted = JsonSerializer.Deserialize<DeduplicationResult>(
            File.ReadAllBytes(resultPath),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new JsonException("The committed deduplication result did not contain an object.");
        var verified = DeduplicationAuthorityDigests.CreateResultDigestMaterial(persisted);
        if (!string.Equals(verified.Result.ResultId, expectedSourceResult.Result.ResultId, StringComparison.Ordinal) ||
            verified.ResultDigest != expectedSourceResult.ResultDigest)
        {
            throw new ResearchWorkspaceConcurrencyException("The supplied source result does not match the committed analysis generation.", new InvalidOperationException());
        }
    }

    private static void RecoverOrphanedAuthorityGenerations(
        ResearchWorkspaceLocation location,
        ResearchWorkspaceProject currentProject)
    {
        var root = ResearchWorkspacePaths.InProject(location.RootDirectory, ResearchWorkspacePaths.AuthorityGenerations);
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var directory in Directory.GetDirectories(root).OrderBy(path => path, StringComparer.Ordinal))
        {
            var generationId = Path.GetFileName(directory);
            if (!string.Equals(generationId, currentProject.CurrentAuthorityGenerationId, StringComparison.Ordinal))
            {
                Quarantine(location, directory, generationId);
            }
        }
    }

    private static ResearchEvent BuildBaselinePublicationEvent(
        ResearchWorkspaceProject project,
        string analysisGenerationId,
        ContentDigest analysisManifestDigest,
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedDeduplicationAuthorityPolicy policy,
        VerifiedCorpusSnapshot snapshot,
        string publisherActorId,
        IClock clock,
        IIdGenerator idGenerator)
    {
        var snapshotRef = new ProvenanceEntityRef("nexus.corpus.snapshot", snapshot.SnapshotId, snapshot.RecordDigest);
        return ResearchEventFactory.Create(
            idGenerator,
            clock,
            new ProvenanceActivity("corpus-snapshot-published", "Corpus snapshot published", true, true, true),
            snapshotRef,
            new ProvenanceAgent(publisherActorId, ProvenanceAgent.HumanKind),
            new[]
            {
                new ProvenanceEntityRef("nexus.deduplication.result", sourceResult.Result.ResultId, sourceResult.ResultDigest),
                new ProvenanceEntityRef(DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind, policy.PolicyId, policy.PolicyDigest),
                new ProvenanceEntityRef("source-analysis-manifest", analysisGenerationId, analysisManifestDigest),
                new ProvenanceEntityRef("deduplication-decision-set", "decision-set-empty", snapshot.DecisionSetDigest)
            },
            new[] { snapshotRef });
    }

    private static byte[] SerializeAuthorityManifest(ResearchWorkspaceAuthorityGenerationManifest manifest)
    {
        var canonical = new CanonicalJsonObject()
            .Add("schema", manifest.Schema)
            .Add("authority_generation_id", manifest.AuthorityGenerationId)
            .Add("workspace_id", manifest.WorkspaceId)
            .Add("project_revision", manifest.ProjectRevision)
            .Add("source_analysis_generation_id", manifest.SourceAnalysisGenerationId)
            .Add("source_analysis_manifest_sha256", manifest.SourceAnalysisManifestSha256)
            .Add("source_result_id", manifest.SourceResultId)
            .Add("source_result_digest", manifest.SourceResultDigest)
            .Add("predecessor_authority_generation_id", CanonicalJsonValue.Null())
            .Add("predecessor_authority_generation_manifest_sha256", CanonicalJsonValue.Null())
            .Add("authority_policy_id", manifest.AuthorityPolicyId)
            .Add("authority_policy_digest", manifest.AuthorityPolicyDigest)
            .Add("decision_set_digest", manifest.DecisionSetDigest)
            .Add("artifacts", CanonicalJsonValue.Array(manifest.Artifacts.OrderBy(item => item.Name, StringComparer.Ordinal)
                .Select(item => (CanonicalJsonValue)new CanonicalJsonObject()
                    .Add("name", item.Name)
                    .Add("relative_path", item.RelativePath)
                    .Add("sha256", item.Sha256)).ToArray()));
        return CanonicalJsonSerializer.SerializeToUtf8Bytes(canonical);
    }

    private static void VerifyStagedAuthorityManifest(
        string stagingRoot,
        string generationRelative,
        byte[] manifestBytes,
        ResearchWorkspaceAuthorityGenerationManifest manifest)
    {
        using var document = System.Text.Json.JsonDocument.Parse(manifestBytes);
        var canonical = CanonicalJsonSerializer.SerializeToUtf8Bytes(CanonicalJsonValue.FromJsonElement(document.RootElement));
        if (!manifestBytes.SequenceEqual(canonical) || manifest.Artifacts.Count != 3)
        {
            throw new InvalidOperationException("Authority generation manifest is not canonical or complete.");
        }

        foreach (var artifact in manifest.Artifacts.OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            var prefix = generationRelative + "/";
            if (!artifact.RelativePath.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Authority artifact path is outside the staged generation.");
            }

            var local = artifact.RelativePath[prefix.Length..];
            var path = Path.Combine(stagingRoot, local.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path) || ContentDigest.Sha256(File.ReadAllBytes(path)).ToString() != artifact.Sha256)
            {
                throw new InvalidOperationException($"Authority artifact '{artifact.Name}' failed staged verification.");
            }
        }
    }

    private static void RejectActiveAuthority(ResearchWorkspaceProject project)
    {
        if (project.CurrentAuthorityGenerationId is not null)
        {
            throw new ResearchWorkspaceAuthorityGenerationActiveException();
        }
    }

    private static FileStream AcquireLock(ResearchWorkspaceLocation location)
    {
        var path = Path.Combine(location.RootDirectory, ResearchWorkspacePaths.ProjectLockFileName);
        try
        {
            return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException exception)
        {
            throw new ResearchWorkspaceConcurrencyException("The workspace is locked by another mutation.", exception);
        }
    }

    private static void Quarantine(ResearchWorkspaceLocation location, string generationRoot, string generationId)
    {
        var quarantine = ResearchWorkspacePaths.InProject(location.RootDirectory, $"{ResearchWorkspacePaths.GenerationQuarantine}/{generationId}");
        Directory.CreateDirectory(Path.GetDirectoryName(quarantine)!);
        Directory.Move(generationRoot, quarantine);
    }
}

public enum ResearchWorkspaceAuthorityFaultPoint
{
    AfterStaging,
    AfterPromotion
}

public sealed record ResearchWorkspaceAuthorityCommit(
    ResearchWorkspaceProject Project,
    ResearchWorkspaceAuthorityGenerationManifest Manifest,
    VerifiedCorpusSnapshot BaselineSnapshot,
    ResearchEvent PublicationEvent);

public sealed class ResearchWorkspaceConcurrencyException : InvalidOperationException
{
    public ResearchWorkspaceConcurrencyException(long expectedRevision, long actualRevision)
        : base($"Workspace revision changed during mutation. Expected {expectedRevision}; found {actualRevision}.")
    {
    }

    public ResearchWorkspaceConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
