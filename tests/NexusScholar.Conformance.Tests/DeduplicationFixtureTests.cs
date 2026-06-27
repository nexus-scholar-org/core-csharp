using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Kernel;

namespace NexusScholar.Conformance.Tests;

[TestClass]
public sealed class DeduplicationFixtureTests
{
    private const string FixtureSourceKind = "local-gate-9-dedup-implementation";
    private const string FixtureSourceCommit = "local-gate-9-dedup-local";
    private static readonly string[] RequiredFixtureIds =
    {
        "dedup-exact-doi-cluster",
        "dedup-exact-cross-provider-id-cluster",
        "dedup-transitive-cluster",
        "dedup-fuzzy-title-review-required",
        "dedup-threshold-95-boundary",
        "dedup-no-id-title-only-no-auto-merge",
        "dedup-representative-election",
        "dedup-representative-merge-preserves-evidence",
        "dedup-raw-sightings-preserved",
        "dedup-web-app-projection-not-authority"
    };

    private static readonly string FixtureDirectory =
        Path.Combine(AppContext.BaseDirectory, "fixtures", "deduplication");

    [TestMethod]
    public void Gate_9_dedup_fixture_files_are_present()
    {
        Directory.CreateDirectory(FixtureDirectory);
        var files = Directory.GetFiles(FixtureDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var fixtureId in RequiredFixtureIds)
        {
            Assert.IsTrue(files.Contains(fixtureId), $"Missing fixture '{fixtureId}.json'.");
        }
    }

    [TestMethod]
    public void Gate_9_dedup_fixtures_have_required_local_metadata()
    {
        foreach (var fixtureId in RequiredFixtureIds)
        {
            using var document = LoadFixture($"{fixtureId}.json");
            var root = document.RootElement;

            Assert.AreEqual(FixtureSourceKind, root.GetProperty("sourceKind").GetString(), fixtureId);
            Assert.AreEqual(FixtureSourceCommit, root.GetProperty("sourceCommit").GetString(), fixtureId);
            Assert.AreEqual("hand-authored local Gate 9 Dedup fixture", root.GetProperty("generatorCommand").GetString(), fixtureId);
            Assert.AreEqual("gate-9-dedup-v1", root.GetProperty("generatorVersion").GetString(), fixtureId);

            var sourceRefs = root.GetProperty("sourceRefs").EnumerateArray().Select(entry => entry.GetString()).ToArray();
            Assert.IsTrue(sourceRefs.Contains("docs/adr/0012-deduplication-evidence-and-cluster-contract.md", StringComparer.Ordinal), fixtureId);

            var comparisonRules = root.GetProperty("comparisonRules").EnumerateArray().Select(entry => entry.GetString()).ToArray();
            Assert.IsTrue(comparisonRules.Contains("no-php-compatibility-claim", StringComparer.Ordinal), fixtureId);
            Assert.IsTrue(comparisonRules.Contains("no-generated-php-fixture", StringComparer.Ordinal), fixtureId);

            _ = ContentDigest.Parse(root.GetProperty("inputDigest").GetString()!);
            _ = ContentDigest.Parse(root.GetProperty("outputDigest").GetString()!);
            Assert.IsTrue(root.TryGetProperty("case", out var _), fixtureId);
        }
    }

    private static JsonDocument LoadFixture(string fileName)
    {
        var path = Path.Combine(FixtureDirectory, fileName);
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
