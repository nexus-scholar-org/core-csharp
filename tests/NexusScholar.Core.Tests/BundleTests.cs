using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Bundles;
using NexusScholar.Kernel;
using NexusScholar.Shared;

namespace NexusScholar.Core.Tests;

[TestClass]
public sealed class BundleTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 6, 27, 1, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Raw_artifact_byte_digest_uses_exact_bytes()
    {
        var lf = Encoding.UTF8.GetBytes("line\n");
        var crlf = Encoding.UTF8.GetBytes("line\r\n");

        Assert.AreEqual(DigestScope.RawArtifactBytes, BundleArtifactEntry.RawByteDigestScope);
        Assert.AreNotEqual(
            BundleArtifactEntry.ComputeRawByteDigest(lf),
            BundleArtifactEntry.ComputeRawByteDigest(crlf));
    }

    [TestMethod]
    public void Logical_path_validator_rejects_escape_and_platform_paths()
    {
        var invalidPaths = new[]
        {
            string.Empty,
            "/absolute/path.json",
            "C:/absolute/path.json",
            "https://example.test/artifact.json",
            "artifacts\\artifact.json",
            "artifacts/../artifact.json",
            "artifacts/./artifact.json",
            "artifacts//artifact.json",
            "artifacts/"
        };

        foreach (var path in invalidPaths)
        {
            Assert.IsFalse(BundleArtifactPath.TryValidate(path, out _), path);
            Assert.ThrowsExactly<ArgumentException>(() => CreateArtifact(path: path), path);
        }

        Assert.AreEqual("artifacts/search-plan.json", BundleArtifactPath.Normalize(" artifacts/search-plan.json "));
    }

    [TestMethod]
    public void Bundle_manifest_digest_uses_bundle_manifest_scope()
    {
        var manifest = CreateManifest();
        var envelope = manifest.ToDigestEnvelope();

        Assert.AreEqual(DigestScope.BundleManifest, envelope.Scope);
        Assert.AreEqual(BundleConstants.ManifestSchemaId, envelope.SchemaId);
        Assert.AreEqual(BundleConstants.ManifestSchemaVersion, envelope.SchemaVersion);
        Assert.AreEqual(envelope.ComputeDigest(), manifest.ComputeManifestDigest());
        Assert.AreEqual(DigestScope.ProtocolContent, BundleProtocolBinding.ProtocolContentDigestScope);
        Assert.AreEqual(DigestScope.ProvenanceEvent, BundleProvenanceBinding.EventDigestScope);
    }

    [TestMethod]
    public void Manifest_digest_is_stable_despite_input_ordering()
    {
        var first = CreateManifest(
            artifacts: new[]
            {
                CreateArtifact("z-artifact", "artifacts/z.json", "z"),
                CreateArtifact("a-artifact", "artifacts/a.json", "a")
            },
            requiredSchemas: new[]
            {
                new BundleSchemaRef("z.schema", "1.0.0"),
                new BundleSchemaRef("a.schema", "1.0.0")
            },
            provenanceBindings: new[]
            {
                new BundleProvenanceBinding("event-z", ContentDigest.Sha256Utf8("event-z"), "z", FixedTime, "actor-1"),
                new BundleProvenanceBinding("event-a", ContentDigest.Sha256Utf8("event-a"), "a", FixedTime, "actor-1")
            },
            sharedIdentityMembership: new[]
            {
                new BundleSharedIdentityMembership(ScholarlyWork.Identified("Second", WorkIdSet.From(WorkId.From("s2", "S2-2")))),
                new BundleSharedIdentityMembership(ScholarlyWork.Identified("First", WorkIdSet.From(WorkId.From("doi", "10.1000/XYZ"))))
            },
            unresolvedCandidates: new[]
            {
                new BundleUnresolvedCandidate(ScholarlyWork.UnresolvedCandidate("Candidate B", "import:row-b"), "candidate-b"),
                new BundleUnresolvedCandidate(ScholarlyWork.UnresolvedCandidate("Candidate A", "import:row-a"), "candidate-a")
            });
        var second = CreateManifest(
            artifacts: first.Artifacts.Reverse(),
            requiredSchemas: first.RequiredSchemas.Reverse(),
            provenanceBindings: first.ProvenanceBindings.Reverse(),
            sharedIdentityMembership: first.SharedIdentityMembership.Reverse(),
            unresolvedCandidates: first.UnresolvedCandidates.Reverse());

        Assert.AreEqual(first.ComputeManifestDigest(), second.ComputeManifestDigest());
        CollectionAssert.AreEqual(
            new[] { "artifacts/a.json", "artifacts/z.json" },
            first.Artifacts.Select(artifact => artifact.LogicalPath).ToArray());
    }

    [TestMethod]
    public void Manifest_digest_changes_when_authoritative_content_changes()
    {
        var first = CreateManifest();
        var changed = CreateManifest(artifacts: new[] { CreateArtifact(content: "changed") });

        Assert.AreNotEqual(first.ComputeManifestDigest(), changed.ComputeManifestDigest());
    }

    [TestMethod]
    public void Verifier_accepts_valid_manifest_and_preserves_immutable_result()
    {
        var artifact = CreateArtifact();
        var manifest = CreateManifest(artifacts: new[] { artifact });
        var verification = new BundleVerifier().Verify(manifest, CreateOptions(artifact, ArtifactBytes()));

        Assert.IsTrue(verification.IsValid);
        Assert.AreEqual(artifact.RawByteDigest, verification.VerifiedArtifacts[0].RawByteDigest);
        Assert.IsFalse(verification.Errors is BundleVerificationFinding[]);
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<BundleVerificationFinding>)verification.Errors).Add(new BundleVerificationFinding("x", "x")));
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<BundleArtifactEntry>)verification.VerifiedArtifacts)[0] = artifact);
    }

    [TestMethod]
    public void Verifier_rejects_duplicate_logical_paths()
    {
        var artifact = CreateArtifact();
        var duplicate = CreateArtifact(artifactRef: "artifact-2");
        var result = new BundleVerifier().Verify(
            CreateManifest(artifacts: new[] { artifact, duplicate }),
            CreateOptions(artifact, ArtifactBytes()));

        AssertHasCategory(result, BundleErrorCodes.DuplicateArtifactPath);
    }

    [TestMethod]
    public void Verifier_rejects_negative_size_and_invalid_digest()
    {
        var bytes = ArtifactBytes();
        var digest = BundleArtifactEntry.ComputeRawByteDigest(bytes);
        var negative = new ReviewBundleManifest(
            "1.0.0",
            "project-1",
            ProtocolDigest(),
            "workflow-1",
            FixedTime,
            new[] { new BundleArtifact("artifacts/search-plan.json", "application/json", -1, digest) });
        var invalidDigest = new ReviewBundleManifest(
            "1.0.0",
            "project-1",
            ProtocolDigest(),
            "workflow-1",
            FixedTime,
            new[] { new BundleArtifact("artifacts/search-plan.json", "application/json", bytes.Length, default) });

        AssertHasCategory(
            new BundleVerifier().Verify(negative, CreateOptions("artifacts/search-plan.json", bytes)),
            BundleErrorCodes.NegativeArtifactSize);
        AssertHasCategory(
            new BundleVerifier().Verify(invalidDigest, CreateOptions("artifacts/search-plan.json", bytes)),
            BundleErrorCodes.InvalidArtifactDigest);
    }

    [TestMethod]
    public void Verifier_rejects_invalid_path_missing_artifact_checksum_mismatch_and_size_mismatch()
    {
        var bytes = ArtifactBytes();
        var validDigest = BundleArtifactEntry.ComputeRawByteDigest(bytes);
        var invalidPath = new ReviewBundleManifest(
            "1.0.0",
            "project-1",
            ProtocolDigest(),
            "workflow-1",
            FixedTime,
            new[] { new BundleArtifact("/escape.json", "application/json", bytes.Length, validDigest) });
        var missing = CreateManifest();
        var checksumMismatchArtifact = CreateArtifact();
        var checksumMismatch = CreateManifest(artifacts: new[] { checksumMismatchArtifact });
        var sizeMismatchArtifact = CreateArtifact(sizeBytes: bytes.Length + 1, rawByteDigest: validDigest);
        var sizeMismatch = CreateManifest(artifacts: new[] { sizeMismatchArtifact });

        AssertHasCategory(
            new BundleVerifier().Verify(invalidPath, CreateOptions("/escape.json", bytes)),
            BundleErrorCodes.InvalidArtifactPath);
        AssertHasCategory(new BundleVerifier().Verify(missing), BundleErrorCodes.MissingArtifact);
        AssertHasCategory(
            new BundleVerifier().Verify(checksumMismatch, CreateOptions(checksumMismatchArtifact, Encoding.UTF8.GetBytes("different"))),
            BundleErrorCodes.ChecksumMismatch);
        AssertHasCategory(
            new BundleVerifier().Verify(sizeMismatch, CreateOptions(sizeMismatchArtifact, bytes)),
            BundleErrorCodes.ArtifactSizeMismatch);
    }

    [TestMethod]
    public void Verifier_rejects_unsupported_schema_stale_digest_workflow_mismatch_provenance_mismatch_and_overwrite()
    {
        var artifact = CreateArtifact();
        var manifest = CreateManifest(
            artifacts: new[] { artifact },
            requiredSchemas: new[] { new BundleSchemaRef("unsupported.schema", "1.0.0") });
        var staleDigest = ContentDigest.Sha256Utf8("stale");
        var workflowMismatch = CreateManifest(
            workflowBinding: new BundleWorkflowBinding(
                "workflow-1",
                ContentDigest.Sha256Utf8("workflow"),
                "template-1",
                "1.0.0",
                ContentDigest.Sha256Utf8("template"),
                "wrong-protocol-version",
                ProtocolDigest()));
        var provenanceMismatch = CreateManifest(
            provenanceBindings: new[]
            {
                new BundleProvenanceBinding("event-1", ContentDigest.Sha256Utf8("actual"), "activity", FixedTime, "actor-1")
            });

        var unsupportedResult = new BundleVerifier().Verify(manifest, CreateOptions(artifact, ArtifactBytes()));
        var staleResult = new BundleVerifier().Verify(CreateManifest(), CreateOptions(CreateArtifact(), ArtifactBytes()) with { ExpectedManifestDigest = staleDigest });
        var workflowResult = new BundleVerifier().Verify(workflowMismatch, CreateOptions(CreateArtifact(), ArtifactBytes()));
        var provenanceResult = new BundleVerifier().Verify(
            provenanceMismatch,
            CreateOptions(CreateArtifact(), ArtifactBytes()) with
            {
                KnownProvenanceEventDigests = new Dictionary<string, ContentDigest>(StringComparer.Ordinal)
                {
                    ["event-1"] = ContentDigest.Sha256Utf8("expected")
                }
            });
        var overwriteResult = new BundleVerifier().Verify(
            CreateManifest(),
            CreateOptions(CreateArtifact(), ArtifactBytes()) with
            {
                ExistingArtifactDigests = new Dictionary<string, ContentDigest>(StringComparer.Ordinal)
                {
                    ["artifacts/search-plan.json"] = ContentDigest.Sha256Utf8("existing")
                }
            });

        AssertHasCategory(unsupportedResult, BundleErrorCodes.UnsupportedRequiredSchema);
        AssertHasCategory(staleResult, BundleErrorCodes.StaleManifestDigest);
        AssertHasCategory(workflowResult, BundleErrorCodes.InvalidWorkflowBinding);
        AssertHasCategory(provenanceResult, BundleErrorCodes.InvalidProvenanceBinding);
        AssertHasCategory(overwriteResult, BundleErrorCodes.DestructiveOverwrite);
    }

    [TestMethod]
    public void Roundtrip_local_equality_uses_manifest_digest_and_no_id_candidates_are_not_membership_identity()
    {
        var artifact = CreateArtifact();
        var candidate = ScholarlyWork.UnresolvedCandidate("No id", "import:row-1");
        var first = CreateManifest(
            artifacts: new[] { artifact },
            unresolvedCandidates: new[] { new BundleUnresolvedCandidate(candidate, "candidate-1") });
        var roundTrip = CreateManifest(
            artifacts: first.Artifacts.Reverse(),
            unresolvedCandidates: first.UnresolvedCandidates.Reverse());

        Assert.AreEqual(first.ComputeManifestDigest(), roundTrip.ComputeManifestDigest());
        Assert.ThrowsExactly<ArgumentException>(() => new BundleSharedIdentityMembership(candidate));
    }

    private static byte[] ArtifactBytes(string value = "{\"query\":\"nexus scholar\"}\n") =>
        Encoding.UTF8.GetBytes(value);

    private static ContentDigest ProtocolDigest() => ContentDigest.Sha256Utf8("protocol-content");

    private static BundleArtifactEntry CreateArtifact(
        string artifactRef = "search-plan",
        string path = "artifacts/search-plan.json",
        string content = "{\"query\":\"nexus scholar\"}\n",
        long? sizeBytes = null,
        ContentDigest? rawByteDigest = null)
    {
        var bytes = ArtifactBytes(content);
        return new BundleArtifactEntry(
            artifactRef,
            path,
            "workflow-artifact",
            "application/json",
            sizeBytes ?? bytes.Length,
            rawByteDigest ?? BundleArtifactEntry.ComputeRawByteDigest(bytes),
            "nexus.workflow.artifact",
            "1.0.0",
            requiredFor: "workflow");
    }

    private static ReviewBundleManifest CreateManifest(
        IEnumerable<BundleArtifactEntry>? artifacts = null,
        IEnumerable<BundleSchemaRef>? requiredSchemas = null,
        BundleWorkflowBinding? workflowBinding = null,
        IEnumerable<BundleProvenanceBinding>? provenanceBindings = null,
        IEnumerable<BundleSharedIdentityMembership>? sharedIdentityMembership = null,
        IEnumerable<BundleUnresolvedCandidate>? unresolvedCandidates = null)
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
            provenanceBindings,
            sharedIdentityMembership,
            unresolvedCandidates);
    }

    private static BundleVerificationOptions CreateOptions(BundleArtifactEntry artifact, byte[] bytes)
    {
        return CreateOptions(artifact.LogicalPath, bytes) with
        {
            SupportedRequiredSchemas = new[] { new BundleSchemaRef("nexus.workflow.artifact", "1.0.0") }
        };
    }

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

    private static void AssertHasCategory(BundleVerification verification, string category)
    {
        Assert.IsTrue(
            verification.Errors.Any(error => string.Equals(error.Category, category, StringComparison.Ordinal)),
            $"Expected error category '{category}' but saw: {string.Join(", ", verification.Errors.Select(error => error.Category))}");
    }
}
