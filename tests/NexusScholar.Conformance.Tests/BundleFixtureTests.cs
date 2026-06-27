using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Bundles;
using NexusScholar.Kernel;

namespace NexusScholar.Conformance.Tests;

[TestClass]
public sealed class BundleFixtureTests
{
    private const string FixtureSourceKind = "local-gate-6-contract";
    private static readonly DateTimeOffset FixedTime = new(2026, 6, 27, 1, 0, 0, TimeSpan.Zero);

    private static readonly string[] RequiredArtifactFixtureIds =
    {
        "artifact-raw-byte-digest",
        "artifact-manifest-entry",
        "artifact-invalid-digest",
        "artifact-negative-size",
        "artifact-forbidden-path-absolute",
        "artifact-forbidden-path-traversal"
    };

    private static readonly string[] RequiredBundleFixtureIds =
    {
        "bundle-manifest-minimal",
        "bundle-manifest-with-protocol-workflow-provenance",
        "bundle-manifest-digest-stable",
        "bundle-roundtrip-local-equivalence",
        "bundle-duplicate-artifact-path",
        "bundle-missing-artifact",
        "bundle-checksum-mismatch",
        "bundle-unsupported-required-schema",
        "bundle-stale-manifest-digest",
        "bundle-destructive-overwrite-reject"
    };

    [TestMethod]
    public void Gate_6_bundle_and_artifact_fixtures_are_present()
    {
        AssertFixtureSet(ArtifactFixtureDirectory(), RequiredArtifactFixtureIds);
        AssertFixtureSet(BundleFixtureDirectory(), RequiredBundleFixtureIds);
    }

    [TestMethod]
    public void Gate_6_fixtures_have_required_local_metadata_and_non_claims()
    {
        foreach (var path in Directory.GetFiles(ArtifactFixtureDirectory(), "*.json")
                     .Concat(Directory.GetFiles(BundleFixtureDirectory(), "*.json")))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var fixtureId = root.GetProperty("fixtureId").GetString();

            Assert.AreEqual(FixtureSourceKind, root.GetProperty("sourceKind").GetString(), fixtureId);
            Assert.AreEqual("hand-authored local Gate 6 bundle fixture", root.GetProperty("generatorCommand").GetString(), fixtureId);
            Assert.AreEqual("gate-6-v1", root.GetProperty("generatorVersion").GetString(), fixtureId);
            Assert.IsTrue(root.GetProperty("sourceRefs").EnumerateArray().Any(value =>
                string.Equals(value.GetString(), "docs/adr/0009-portable-bundle-and-artifact-contract.md", StringComparison.Ordinal)), fixtureId);
            Assert.IsTrue(root.GetProperty("comparisonRules").EnumerateArray().Any(value =>
                string.Equals(value.GetString(), "no-php-compatibility-claim", StringComparison.Ordinal)), fixtureId);
            Assert.IsTrue(root.GetProperty("comparisonRules").EnumerateArray().Any(value =>
                string.Equals(value.GetString(), "no-blueprint-conformance-claim", StringComparison.Ordinal)), fixtureId);
            _ = ContentDigest.Parse(root.GetProperty("inputDigest").GetString()!);
            _ = ContentDigest.Parse(root.GetProperty("outputDigest").GetString()!);
        }
    }

    [TestMethod]
    public void Positive_gate_6_fixtures_replay_local_contract()
    {
        using (var rawDigest = LoadFixture(ArtifactFixtureDirectory(), "artifact-raw-byte-digest.json"))
        {
            var root = rawDigest.RootElement;
            var bytes = Encoding.UTF8.GetBytes(root.GetProperty("case").GetProperty("bytesUtf8").GetString()!);
            Assert.AreEqual(
                root.GetProperty("case").GetProperty("expectedDigest").GetString(),
                BundleArtifactEntry.ComputeRawByteDigest(bytes).ToString());
        }

        using (var entryFixture = LoadFixture(ArtifactFixtureDirectory(), "artifact-manifest-entry.json"))
        {
            var entry = CreateArtifact();
            var fixtureCase = entryFixture.RootElement.GetProperty("case");
            Assert.AreEqual(fixtureCase.GetProperty("logicalPath").GetString(), entry.LogicalPath);
            Assert.AreEqual(fixtureCase.GetProperty("digestScope").GetString(), BundleArtifactEntry.RawByteDigestScope.Value);
        }

        foreach (var fixtureId in new[]
                 {
                     "bundle-manifest-minimal",
                     "bundle-manifest-with-protocol-workflow-provenance",
                     "bundle-manifest-digest-stable",
                     "bundle-roundtrip-local-equivalence"
                 })
        {
            using var document = LoadFixture(BundleFixtureDirectory(), $"{fixtureId}.json");
            var manifest = fixtureId == "bundle-manifest-with-protocol-workflow-provenance"
                ? CreateManifest(workflowBinding: CreateWorkflowBinding(), provenanceBindings: new[] { CreateProvenanceBinding() })
                : CreateManifest();
            var verification = new BundleVerifier().Verify(manifest, CreateOptions(CreateArtifact(), ArtifactBytes()));

            Assert.IsTrue(verification.IsValid, fixtureId);
            Assert.AreEqual("bundle-manifest", document.RootElement.GetProperty("case").GetProperty("digestScope").GetString(), fixtureId);
            Assert.IsTrue(document.RootElement.GetProperty("case").GetProperty("nonClaims").EnumerateArray().Any(value =>
                string.Equals(value.GetString(), "no-php-compatibility-claim", StringComparison.Ordinal)), fixtureId);
        }
    }

    [TestMethod]
    public void Negative_gate_6_fixtures_replay_expected_error_categories()
    {
        AssertNegative("artifact-invalid-digest.json", ArtifactFixtureDirectory(), BundleErrorCodes.InvalidArtifactDigest, () =>
            new BundleVerifier().Verify(
                CreateLegacyManifest(new BundleArtifact("artifacts/search-plan.json", "application/json", ArtifactBytes().Length, default)),
                CreateOptions("artifacts/search-plan.json", ArtifactBytes())));

        AssertNegative("artifact-negative-size.json", ArtifactFixtureDirectory(), BundleErrorCodes.NegativeArtifactSize, () =>
            new BundleVerifier().Verify(
                CreateLegacyManifest(new BundleArtifact("artifacts/search-plan.json", "application/json", -1, BundleArtifactEntry.ComputeRawByteDigest(ArtifactBytes()))),
                CreateOptions("artifacts/search-plan.json", ArtifactBytes())));

        AssertNegative("artifact-forbidden-path-absolute.json", ArtifactFixtureDirectory(), BundleErrorCodes.InvalidArtifactPath, () =>
            new BundleVerifier().Verify(
                CreateLegacyManifest(new BundleArtifact("/escape.json", "application/json", ArtifactBytes().Length, BundleArtifactEntry.ComputeRawByteDigest(ArtifactBytes()))),
                CreateOptions("/escape.json", ArtifactBytes())));

        AssertNegative("artifact-forbidden-path-traversal.json", ArtifactFixtureDirectory(), BundleErrorCodes.InvalidArtifactPath, () =>
            new BundleVerifier().Verify(
                CreateLegacyManifest(new BundleArtifact("artifacts/../escape.json", "application/json", ArtifactBytes().Length, BundleArtifactEntry.ComputeRawByteDigest(ArtifactBytes()))),
                CreateOptions("artifacts/../escape.json", ArtifactBytes())));

        AssertNegative("bundle-duplicate-artifact-path.json", BundleFixtureDirectory(), BundleErrorCodes.DuplicateArtifactPath, () =>
        {
            var first = CreateArtifact();
            var second = CreateArtifact(artifactRef: "search-plan-copy");
            return new BundleVerifier().Verify(
                CreateManifest(artifacts: new[] { first, second }),
                CreateOptions(first, ArtifactBytes()));
        });

        AssertNegative("bundle-missing-artifact.json", BundleFixtureDirectory(), BundleErrorCodes.MissingArtifact, () =>
            new BundleVerifier().Verify(CreateManifest()));

        AssertNegative("bundle-checksum-mismatch.json", BundleFixtureDirectory(), BundleErrorCodes.ChecksumMismatch, () =>
            new BundleVerifier().Verify(CreateManifest(), CreateOptions(CreateArtifact(), Encoding.UTF8.GetBytes("different"))));

        AssertNegative("bundle-unsupported-required-schema.json", BundleFixtureDirectory(), BundleErrorCodes.UnsupportedRequiredSchema, () =>
            new BundleVerifier().Verify(
                CreateManifest(requiredSchemas: new[] { new BundleSchemaRef("unsupported.schema", "1.0.0") }),
                CreateOptions(CreateArtifact(), ArtifactBytes())));

        AssertNegative("bundle-stale-manifest-digest.json", BundleFixtureDirectory(), BundleErrorCodes.StaleManifestDigest, () =>
            new BundleVerifier().Verify(
                CreateManifest(),
                CreateOptions(CreateArtifact(), ArtifactBytes()) with { ExpectedManifestDigest = ContentDigest.Sha256Utf8("stale") }));

        AssertNegative("bundle-destructive-overwrite-reject.json", BundleFixtureDirectory(), BundleErrorCodes.DestructiveOverwrite, () =>
            new BundleVerifier().Verify(
                CreateManifest(),
                CreateOptions(CreateArtifact(), ArtifactBytes()) with
                {
                    ExistingArtifactDigests = new Dictionary<string, ContentDigest>(StringComparer.Ordinal)
                    {
                        ["artifacts/search-plan.json"] = ContentDigest.Sha256Utf8("existing")
                    }
                }));
    }

    private static void AssertFixtureSet(string directory, IEnumerable<string> expectedIds)
    {
        var actual = Directory.GetFiles(directory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var expectedId in expectedIds)
        {
            Assert.IsTrue(actual.Contains(expectedId), $"Missing Gate 6 fixture '{expectedId}'.");
        }
    }

    private static void AssertNegative(
        string fileName,
        string directory,
        string expectedCategory,
        Func<BundleVerification> action)
    {
        using var document = LoadFixture(directory, fileName);
        var fixtureCategory = document.RootElement.GetProperty("case").GetProperty("errorCategory").GetString();

        Assert.AreEqual(expectedCategory, fixtureCategory, fileName);

        var verification = action();
        Assert.IsTrue(
            verification.Errors.Any(error => string.Equals(error.Category, expectedCategory, StringComparison.Ordinal)),
            $"{fileName} expected '{expectedCategory}' but saw: {string.Join(", ", verification.Errors.Select(error => error.Category))}");
    }

    private static JsonDocument LoadFixture(string directory, string fileName) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, fileName)));

    private static string ArtifactFixtureDirectory() =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "artifacts");

    private static string BundleFixtureDirectory() =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "bundles");

    private static byte[] ArtifactBytes() => Encoding.UTF8.GetBytes("{\"query\":\"nexus scholar\"}\n");

    private static ContentDigest ProtocolDigest() => ContentDigest.Sha256Utf8("protocol-content");

    private static BundleArtifactEntry CreateArtifact(string artifactRef = "search-plan")
    {
        var bytes = ArtifactBytes();
        return new BundleArtifactEntry(
            artifactRef,
            "artifacts/search-plan.json",
            "workflow-artifact",
            "application/json",
            bytes.Length,
            BundleArtifactEntry.ComputeRawByteDigest(bytes),
            "nexus.workflow.artifact",
            "1.0.0",
            requiredFor: "workflow");
    }

    private static ReviewBundleManifest CreateManifest(
        IEnumerable<BundleArtifactEntry>? artifacts = null,
        IEnumerable<BundleSchemaRef>? requiredSchemas = null,
        BundleWorkflowBinding? workflowBinding = null,
        IEnumerable<BundleProvenanceBinding>? provenanceBindings = null)
    {
        return new ReviewBundleManifest(
            "bundle-1",
            "researcher-1",
            new BundleProtocolBinding(
                "protocol-1",
                "protocol-version-1",
                1,
                BundleConstants.ApprovedProtocolStatus,
                ProtocolDigest()),
            artifacts ?? new[] { CreateArtifact() },
            requiredSchemas ?? new[] { new BundleSchemaRef("nexus.workflow.artifact", "1.0.0") },
            FixedTime,
            workflowBinding,
            provenanceBindings);
    }

    private static ReviewBundleManifest CreateLegacyManifest(BundleArtifact artifact)
    {
        return new ReviewBundleManifest(
            "1.0.0",
            "project-1",
            ProtocolDigest(),
            "workflow-1",
            FixedTime,
            new[] { artifact });
    }

    private static BundleWorkflowBinding CreateWorkflowBinding()
    {
        return new BundleWorkflowBinding(
            "workflow-1",
            ContentDigest.Sha256Utf8("workflow"),
            "template-1",
            "1.0.0",
            ContentDigest.Sha256Utf8("template"),
            "protocol-version-1",
            ProtocolDigest());
    }

    private static BundleProvenanceBinding CreateProvenanceBinding()
    {
        return new BundleProvenanceBinding(
            "event-1",
            ContentDigest.Sha256Utf8("event"),
            "workflow-node-completed",
            FixedTime,
            "researcher-1");
    }

    private static BundleVerificationOptions CreateOptions(BundleArtifactEntry artifact, byte[] bytes) =>
        CreateOptions(artifact.LogicalPath, bytes);

    private static BundleVerificationOptions CreateOptions(string logicalPath, byte[] bytes)
    {
        return new BundleVerificationOptions
        {
            SupportedRequiredSchemas = new[] { new BundleSchemaRef("nexus.workflow.artifact", "1.0.0") },
            ArtifactBytes = new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [logicalPath] = bytes
            }
        };
    }
}
