using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Deduplication;
using NexusScholar.Search;
using NexusScholar.UiContracts;

namespace NexusScholar.AppServices.Tests;

[TestClass]
public sealed class App01GeneratedLocalBundleTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    [TestMethod]
    public void GeneratedLocalBundleManifest_FileDigestsMatch()
    {
        var manifest = LoadManifest();

        foreach (var bundle in manifest.Bundles)
        {
            foreach (var file in bundle.Files)
            {
                var bytes = File.ReadAllBytes(FixturePath(file.Path));
                var digest = $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

                Assert.AreEqual(file.Sha256, digest, $"{bundle.BundleId}: {file.Path}");
                Assert.AreEqual(file.Bytes, bytes.Length, $"{bundle.BundleId}: {file.Path}");
            }
        }
    }

    [TestMethod]
    public void GeneratedLocalBundles_ParseAndDedupMatchManifestExpectations()
    {
        var manifest = LoadManifest();

        foreach (var bundle in manifest.Bundles)
        {
            var context = LoadBundle(bundle);

            AssertExpectedCoverage(bundle, context);
        }
    }

    [TestMethod]
    public void GeneratedLocalCombinedDemo_ComposesAppProjectionPlanWithRequiredBlockTypes()
    {
        var bundle = LoadManifest().Bundles.Single(item => string.Equals(
            item.BundleId,
            "FB07-combined-app01-demo",
            StringComparison.Ordinal));
        var context = LoadBundle(bundle);
        var aggregateTrace = AggregateTrace(bundle, context.Traces);

        var plan = new SearchDedupWorkspacePlanComposer().Compose(
            new SearchDedupWorkspacePlanInput(
                "workspace-fb07-combined-app01-demo",
                bundle.Title,
                aggregateTrace,
                context.DeduplicationResult,
                "Generated local APP-01 bundle projection. Non-authoritative test data only."));

        Assert.AreEqual(BlockMode.Review, plan.Mode);
        Assert.IsTrue(plan.Blocks.All(block => block.SourceKind == BlockSourceKind.AppProjection));
        CollectionAssert.IsSubsetOf(
            new[]
            {
                KnownBlockKinds.ImportSummary,
                KnownBlockKinds.ImportWarningSummary,
                KnownBlockKinds.DedupCandidateCluster,
                KnownBlockKinds.DedupRecordComparison,
                KnownBlockKinds.HumanGateMergeDecision
            },
            plan.Blocks.Select(block => block.Kind).Distinct(StringComparer.Ordinal).ToArray());
        Assert.IsTrue(plan.Blocks.Any(block => block.Kind == KnownBlockKinds.HumanGateMergeDecision && block.Actions.All(action => action.CommandKind is null)));

        var json = JsonSerializer.Serialize(plan, UiContractJson.SerializerOptions);
        Assert.IsFalse(json.Contains("\"Sample\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("C:\\", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(json, "app-projection-only");
    }

    [TestMethod]
    public void GeneratedLocalBundles_AreExplicitlyNonConformanceData()
    {
        var manifest = LoadManifest();
        var readme = File.ReadAllText(FixturePath("README.md"));

        CollectionAssert.Contains(manifest.NonClaims.ToArray(), "not-scopus-export");
        CollectionAssert.Contains(manifest.NonClaims.ToArray(), "not-web-of-science-export");
        CollectionAssert.Contains(manifest.NonClaims.ToArray(), "not-google-scholar-scrape");
        CollectionAssert.Contains(manifest.NonClaims.ToArray(), "not-php-compatibility-fixture");
        StringAssert.Contains(readme, "local APP-01 testing only");
        StringAssert.Contains(readme, "not for scientific analysis or PHP compatibility claims");
    }

    private static BundleContext LoadBundle(BundleManifestEntry bundle)
    {
        var importService = new SearchImportService();
        var traces = bundle.Files
            .Select(file =>
            {
                var request = new SearchImportRequest(
                    file.ImportRequest.SourceDatabaseOrTool,
                    file.ImportRequest.ExportFormat,
                    file.ImportRequest.ParserId,
                    file.ImportRequest.ParserVersion,
                    file.ImportRequest.ImportedBy,
                    file.ImportRequest.ImportedAt,
                    file.ImportRequest.OriginalQueryText,
                    file.ImportRequest.ExportedAt);
                var sourceBytes = File.ReadAllBytes(FixturePath(file.Path));

                return importService.Parse(file.TraceId, request, sourceBytes);
            })
            .ToArray();
        var result = new DeduplicationService().Execute(
            $"dedup-{bundle.BundleId}",
            Array.Empty<SearchTrace>(),
            traces);

        return new BundleContext(traces, result);
    }

    private static void AssertExpectedCoverage(BundleManifestEntry bundle, BundleContext context)
    {
        var actual = BundleCoverage.From(context);
        var expected = bundle.Expected;

        AssertExactOrAtLeast(expected.ImportedRecords, expected.ImportedRecordsAtLeast, actual.ImportedRecords, bundle.BundleId, "imported records");
        AssertExactOrAtLeast(expected.Sightings ?? expected.SightingsExpected, null, actual.Sightings, bundle.BundleId, "sightings");
        AssertExactOrAtLeast(expected.ParserWarnings, expected.ParserWarningsAtLeast, actual.ParserWarnings, bundle.BundleId, "parser warnings");
        AssertExactOrAtLeast(expected.SkippedRecords, expected.SkippedRecordsAtLeast, actual.SkippedRecords, bundle.BundleId, "skipped records");
        AssertExactOrAtLeast(expected.DedupExactClusters, expected.DedupExactClustersAtLeast, actual.ExactClusters, bundle.BundleId, "exact clusters");
        AssertExactOrAtLeast(expected.DedupReviewCandidates, expected.DedupReviewCandidatesAtLeast, actual.ReviewCandidates, bundle.BundleId, "review candidates");
        AssertExactOrAtLeast(expected.UnresolvedCandidates, null, actual.UnresolvedCandidates, bundle.BundleId, "unresolved candidates");

        if (expected.ParserWarningsExpected == true)
        {
            Assert.IsTrue(actual.ParserWarnings > 0, $"{bundle.BundleId}: parser warnings were expected.");
        }

        if (expected.SourceSpecificWarningsExpected == true)
        {
            Assert.IsTrue(
                context.Traces.Any(trace => trace.ParserWarnings.Any(warning => string.Equals(
                    warning.Category,
                    SearchImportErrorCodes.UnknownIdentifierType,
                    StringComparison.Ordinal))),
                $"{bundle.BundleId}: source-specific identifier warnings were expected.");
        }

        if (expected.AcceptedWorkIdNamespacesExpected.Count > 0)
        {
            var actualNamespaces = context.Traces
                .SelectMany(trace => trace.ImportedRecords)
                .SelectMany(record => record.Work.WorkIds.Ids)
                .Select(id => id.Namespace.Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var expectedNamespace in expected.AcceptedWorkIdNamespacesExpected)
            {
                CollectionAssert.Contains(actualNamespaces, expectedNamespace, $"{bundle.BundleId}: accepted WorkId namespace.");
            }
        }

        var suggestedMode = actual.ParserWarnings > 0 || actual.ReviewCandidates > 0
            ? BlockMode.Review
            : BlockMode.Audit;
        Assert.AreEqual(bundle.Expected.SuggestedBlockMode, suggestedMode.ToString(), $"{bundle.BundleId}: suggested block mode.");
    }

    private static SearchImportTrace AggregateTrace(BundleManifestEntry bundle, IReadOnlyList<SearchImportTrace> traces)
    {
        var parserWarnings = traces
            .SelectMany(trace => trace.ParserWarnings)
            .OrderBy(warning => warning.Category, StringComparer.Ordinal)
            .ThenBy(warning => warning.SourceRecordId, StringComparer.Ordinal)
            .ThenBy(warning => warning.RecordIndex)
            .ThenBy(warning => warning.Message, StringComparer.Ordinal)
            .ToArray();
        var importedRecords = traces
            .SelectMany(trace => trace.ImportedRecords)
            .ToArray();
        var sightings = traces
            .SelectMany(trace => trace.Sightings)
            .ToArray();
        var sourceDigest = ComputeDigest(string.Join(
            "\n",
            traces.Select(trace => trace.Metadata.SourceFileDigest).OrderBy(value => value, StringComparer.Ordinal)));
        var metadata = new SearchImportMetadata(
            SearchImportMetadata.AcquisitionKindImportedExport,
            "app01-generated-local-bundle",
            "mixed-local-fixture",
            "app01-generated-local-bundle-test-harness",
            "1.0.0",
            sourceDigest,
            "app01-generated-local-bundle-digests",
            "app01-generated-local-bundle-test",
            "2026-07-01T00:00:00Z",
            bundle.Purpose,
            "2026-07-01T00:00:00Z",
            importedRecords.Length,
            parserWarnings);

        return new SearchImportTrace(
            $"trace-{bundle.BundleId.ToLowerInvariant()}-aggregate",
            "nexus.search.import.trace",
            "1.0.0",
            metadata,
            importedRecords,
            sightings,
            parserWarnings,
            SearchImportTrace.DefaultNonClaims);
    }

    private static void AssertExactOrAtLeast(
        int? exact,
        int? atLeast,
        int actual,
        string bundleId,
        string label)
    {
        if (exact.HasValue)
        {
            Assert.AreEqual(exact.Value, actual, $"{bundleId}: {label}");
        }

        if (atLeast.HasValue)
        {
            Assert.IsTrue(actual >= atLeast.Value, $"{bundleId}: expected at least {atLeast.Value} {label}, got {actual}.");
        }
    }

    private static BundleManifest LoadManifest()
    {
        var manifest = JsonSerializer.Deserialize<BundleManifest>(
            File.ReadAllText(FixturePath("manifest.json")),
            JsonOptions);
        Assert.IsNotNull(manifest);

        return manifest;
    }

    private static string FixturePath(string relativePath) =>
        Path.Combine(FixtureRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string FixtureRoot() =>
        Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "NexusScholar.AppServices.Tests",
            "Fixtures",
            "App01GeneratedLocalBundles");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NexusScholar.Core.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }

    private static string ComputeDigest(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"sha256:{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    private sealed record BundleContext(
        IReadOnlyList<SearchImportTrace> Traces,
        DeduplicationResult DeduplicationResult);

    private sealed record BundleCoverage(
        int ImportedRecords,
        int SkippedRecords,
        int Sightings,
        int ParserWarnings,
        int ExactClusters,
        int ReviewCandidates,
        int UnresolvedCandidates)
    {
        public static BundleCoverage From(BundleContext context) =>
            new(
                context.Traces.Sum(trace => trace.ImportedRecords.Count),
                context.Traces.Sum(trace => trace.ImportedRecords.Count(record => record.IsSkipped)),
                context.Traces.Sum(trace => trace.Sightings.Count),
                context.Traces.Sum(trace => trace.ParserWarnings.Count),
                context.DeduplicationResult.Clusters.Count,
                context.DeduplicationResult.ReviewRequiredCandidates.Count,
                context.DeduplicationResult.UnresolvedCandidates.Count);
    }

    private sealed record BundleManifest(
        string PackId,
        string GeneratedAt,
        string Generator,
        string Scope,
        IReadOnlyList<string> NonClaims,
        ParserDefaults ParserDefaults,
        IReadOnlyList<BundleManifestEntry> Bundles);

    private sealed record ParserDefaults(
        string ParserId,
        string ParserVersion,
        string ImportedBy,
        string ImportedAt,
        string ExportedAt);

    private sealed record BundleManifestEntry(
        string BundleId,
        string Title,
        string Purpose,
        IReadOnlyList<BundleFileEntry> Files,
        BundleExpected Expected,
        IReadOnlyList<string> Notes);

    private sealed record BundleFileEntry(
        string Path,
        string SourceDatabaseOrTool,
        string ExportFormat,
        string TraceId,
        string OriginalQueryText,
        string Sha256,
        int Bytes,
        BundleImportRequest ImportRequest);

    private sealed record BundleImportRequest(
        string SourceDatabaseOrTool,
        string ExportFormat,
        string ParserId,
        string ParserVersion,
        string ImportedBy,
        string ImportedAt,
        string? OriginalQueryText,
        string? ExportedAt);

    private sealed record BundleExpected
    {
        public int? ImportedRecords { get; init; }
        public int? ImportedRecordsAtLeast { get; init; }
        public int? Sightings { get; init; }
        public int? SightingsExpected { get; init; }
        public int? ParserWarnings { get; init; }
        public int? ParserWarningsAtLeast { get; init; }
        public bool? ParserWarningsExpected { get; init; }
        public int? SkippedRecords { get; init; }
        public int? SkippedRecordsAtLeast { get; init; }
        public int? DedupExactClusters { get; init; }
        public int? DedupExactClustersAtLeast { get; init; }
        public int? DedupReviewCandidates { get; init; }
        public int? DedupReviewCandidatesAtLeast { get; init; }
        public int? UnresolvedCandidates { get; init; }
        public bool? SourceSpecificWarningsExpected { get; init; }
        public IReadOnlyList<string> AcceptedWorkIdNamespacesExpected { get; init; } = Array.Empty<string>();
        public string SuggestedBlockMode { get; init; } = string.Empty;
    }
}
