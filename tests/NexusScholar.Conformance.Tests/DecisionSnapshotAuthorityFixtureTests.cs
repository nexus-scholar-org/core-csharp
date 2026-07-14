using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;
using NexusScholar.Provenance;
using NexusScholar.ResearchWorkspace;

namespace NexusScholar.Conformance.Tests;

[TestClass]
public sealed class DecisionSnapshotAuthorityFixtureTests
{
    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2026, 7, 14, 16, 0, 0, TimeSpan.Zero));

    [TestMethod]
    public void DecisionSnapshotAuthority_valid_fixture_replays_byte_identically()
    {
        var generated = Generate();
        if (Environment.GetEnvironmentVariable("UPDATE_FE01_FIXTURES") == "1")
        {
            WriteFixtures(generated);
        }

        foreach (var item in generated)
        {
            CollectionAssert.AreEqual(File.ReadAllBytes(FixturePath(item.Key)), item.Value, item.Key);
        }

        var policy = ResearchWorkspaceAuthorityArtifacts.VerifyPolicyCanonicalRecord(generated["authority-policy.json"]);
        var source = BuildSource();
        var snapshot = ResearchWorkspaceAuthorityArtifacts.VerifySnapshotCanonicalRecord(generated["baseline-snapshot.json"], source, policy);
        var researchEvent = ResearchWorkspaceAuthorityArtifacts.VerifyResearchEventCanonicalRecord(generated["snapshot-publication-event.json"]);
        Assert.AreEqual(0, snapshot.DecisionReferences.Count);
        Assert.AreEqual(snapshot.RecordDigest, researchEvent.Outputs.Single().Digest);

        using var manifest = JsonDocument.Parse(generated["manifest.json"]);
        Assert.AreEqual("nexus.fe01.local-conformance.v1", manifest.RootElement.GetProperty("schema").GetString());
        foreach (var file in manifest.RootElement.GetProperty("files").EnumerateArray())
        {
            Assert.AreEqual(
                file.GetProperty("sha256").GetString(),
                ContentDigest.Sha256(generated[file.GetProperty("name").GetString()!]).ToString());
        }
    }

    [TestMethod]
    public void DecisionSnapshotAuthority_tampered_snapshot_fails_stable_category()
    {
        var generated = Generate();
        using var document = JsonDocument.Parse(generated["baseline-snapshot.json"]);
        var root = (CanonicalJsonObject)CanonicalJsonValue.FromJsonElement(document.RootElement);
        using var snapshotDocument = JsonDocument.Parse(generated["baseline-snapshot.json"]);
        var recordedDigest = snapshotDocument.RootElement.GetProperty("record_digest").GetString()!;
        var tampered = System.Text.Encoding.UTF8.GetBytes(
            System.Text.Encoding.UTF8.GetString(generated["baseline-snapshot.json"])
                .Replace(recordedDigest, ContentDigest.Sha256Utf8("tampered").ToString(), StringComparison.Ordinal));

        var error = Assert.ThrowsExactly<CorpusSnapshotAuthorityException>(() =>
            ResearchWorkspaceAuthorityArtifacts.VerifySnapshotCanonicalRecord(tampered, BuildSource(), BuildPolicy()));
        Assert.AreEqual(CorpusSnapshotErrorCodes.InvalidSnapshot, error.Category);
    }

    private static Dictionary<string, byte[]> Generate()
    {
        var policy = BuildPolicy();
        var source = BuildSource();
        var snapshot = CorpusSnapshotService.CreateBaseline("snapshot-fe01-fixture", source, policy, "alice", "owner", Clock);
        var snapshotRef = new ProvenanceEntityRef("nexus.corpus.snapshot", snapshot.SnapshotId, snapshot.RecordDigest);
        var researchEvent = ResearchEventFactory.Create(
            new FixedIdGenerator(Guid.Parse("00000000-0000-0000-0000-000000000801")),
            Clock,
            new ProvenanceActivity("corpus-snapshot-published", "Corpus snapshot published", true, true, true),
            snapshotRef,
            new ProvenanceAgent("alice", ProvenanceAgent.HumanKind),
            new[]
            {
                new ProvenanceEntityRef("nexus.deduplication.result", source.Result.ResultId, source.ResultDigest),
                new ProvenanceEntityRef(DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind, policy.PolicyId, policy.PolicyDigest),
                new ProvenanceEntityRef("source-analysis-manifest", "gen-fe01-fixture", ContentDigest.Sha256Utf8("analysis-manifest")),
                new ProvenanceEntityRef("deduplication-decision-set", "decision-set-empty", snapshot.DecisionSetDigest)
            },
            new[] { snapshotRef });

        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["authority-policy.json"] = ResearchWorkspaceAuthorityArtifacts.SerializePolicyCanonicalRecord(policy),
            ["baseline-snapshot.json"] = ResearchWorkspaceAuthorityArtifacts.SerializeSnapshotCanonicalRecord(snapshot),
            ["snapshot-publication-event.json"] = ResearchWorkspaceAuthorityArtifacts.SerializeResearchEventCanonicalRecord(researchEvent)
        };
        var manifest = new CanonicalJsonObject()
            .Add("schema", "nexus.fe01.local-conformance.v1")
            .Add("canonicalization_profile", CanonicalJsonSerializer.ProfileId)
            .Add("generator", "UPDATE_FE01_FIXTURES=1 dotnet test --filter FullyQualifiedName~DecisionSnapshotAuthority")
            .Add("source", "ADR 0028 local C# contract; no PHP compatibility claim")
            .Add("files", CanonicalJsonValue.Array(files.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => (CanonicalJsonValue)new CanonicalJsonObject()
                    .Add("name", item.Key)
                    .Add("sha256", ContentDigest.Sha256(item.Value).ToString())).ToArray()));
        files["manifest.json"] = CanonicalJsonSerializer.SerializeToUtf8Bytes(manifest);
        return files;
    }

    private static VerifiedDeduplicationAuthorityPolicy BuildPolicy() =>
        DeduplicationAuthorityPolicy.CreatePolicyMaterial(new UnverifiedDeduplicationAuthorityPolicy(
            DeduplicationAuthorityPolicyConstants.SchemaId,
            DeduplicationAuthorityPolicyConstants.SchemaVersion,
            DeduplicationAuthorityPolicyConstants.LocalAuthoritySourceKind,
            DeduplicationService.PolicyId,
            DeduplicationService.PolicyVersion,
            new[] { new DeduplicationAuthorityPolicyActorRole("alice", "owner") },
            DeduplicationAuthorityPolicyConstants.ClosedActions,
            new[]
            {
                new DeduplicationAuthorityPolicyReasonGroup(DeduplicationAuthorityPolicyConstants.MergeAction, new[] { "duplicate" }),
                new DeduplicationAuthorityPolicyReasonGroup(DeduplicationAuthorityPolicyConstants.KeepSeparateAction, new[] { "different" }),
                new DeduplicationAuthorityPolicyReasonGroup(DeduplicationAuthorityPolicyConstants.MarkUnresolvedAction, new[] { "uncertain" })
            },
            false,
            "alice",
            "owner",
            Clock.UtcNow));

    private static VerifiedDeduplicationAuthorityResultDigest BuildSource()
    {
        var stable = Candidate("candidate-stable", true);
        var unresolved = Candidate("candidate-unresolved", false);
        var result = new DeduplicationResult(
            "result-fe01-fixture",
            DeduplicationAuthorityDigests.ResultSchemaId,
            DeduplicationAuthorityDigests.ResultSchemaVersion,
            DeduplicationService.PolicyId,
            DeduplicationService.PolicyVersion,
            0.95,
            new Dictionary<string, int>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { stable, unresolved },
            Array.Empty<DedupCluster>(),
            Array.Empty<DedupEvidence>(),
            new[] { unresolved },
            Array.Empty<DedupReviewCandidate>(),
            Array.Empty<DedupMessage>(),
            Array.Empty<DedupMessage>(),
            Array.Empty<string>());
        return DeduplicationAuthorityDigests.CreateResultDigestMaterial(result);
    }

    private static DedupCandidateRecord Candidate(string id, bool stable) => new(
        id,
        $"Title {id}",
        stable,
        stable ? $"doi-{id}" : null,
        stable ? new[] { $"work-{id}" } : Array.Empty<string>(),
        new[] { $"record-{id}" },
        new DedupSightingRef("fixture", $"trace-{id}", SourceSightingId: $"sighting-{id}", ProviderAlias: "fixture", SourceDatabaseOrTool: "fixture"));

    private static string FixturePath(string name) => Environment.GetEnvironmentVariable("UPDATE_FE01_FIXTURES") == "1"
        ? Path.Combine(RepositoryRoot(), "fixtures", "conformance", "decision-snapshot-authority", name)
        : Path.Combine(AppContext.BaseDirectory, "fixtures", "decision-snapshot-authority", name);

    private static void WriteFixtures(IReadOnlyDictionary<string, byte[]> files)
    {
        var directory = Path.Combine(RepositoryRoot(), "fixtures", "conformance", "decision-snapshot-authority");
        Directory.CreateDirectory(directory);
        foreach (var item in files)
        {
            File.WriteAllBytes(Path.Combine(directory, item.Key), item.Value);
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NexusScholar.Core.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class FixedClock(DateTimeOffset value) : IClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }

    private sealed class FixedIdGenerator(Guid value) : IIdGenerator
    {
        public Guid NewId() => value;
    }
}
