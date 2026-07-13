using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Search;
using NexusScholar.Shared;

namespace NexusScholar.Conformance.Tests;

[TestClass]
public sealed class PhpSearchGoldenTests
{
    private const string FixtureSetId = "php-search-v1";

    [TestMethod]
    public void Manifest_binds_pinned_source_and_all_evidence_files()
    {
        using var manifest = Load("manifest.json");
        using var sourceLock = JsonDocument.Parse(File.ReadAllBytes(SourceLockPath()));
        var root = manifest.RootElement;
        var phpReference = sourceLock.RootElement.GetProperty("php_reference");

        Assert.AreEqual(FixtureSetId, root.GetProperty("fixtureSetId").GetString());
        Assert.AreEqual("pinned-php-observable-behavior", root.GetProperty("sourceKind").GetString());
        Assert.AreEqual(phpReference.GetProperty("repository").GetString(), root.GetProperty("sourceRepository").GetString());
        Assert.AreEqual(phpReference.GetProperty("commit").GetString(), root.GetProperty("sourceCommit").GetString());
        Assert.AreEqual("1.0.0", root.GetProperty("schemaVersion").GetString());
        Assert.AreEqual("search-v1", root.GetProperty("generatorVersion").GetString());
        Assert.AreEqual(
            "php scripts/php-golden/search-export.php --php-reference \"$PHP_REFERENCE\" --source-lock specs/SOURCE.lock.json --input fixtures/php-golden/search/v1/input.json --comparison fixtures/php-golden/search/v1/comparison.json --output fixtures/php-golden/search/v1/expected.json --manifest fixtures/php-golden/search/v1/manifest.json",
            root.GetProperty("generatorCommand").GetString());
        CollectionAssert.AreEqual(new[]
        {
            "src/Search/Domain/SearchTerm.php",
            "src/Search/Domain/YearRange.php",
            "src/Search/Domain/SearchQuery.php",
            "src/Search/Domain/Port/AdapterCollection.php",
            "src/Search/Application/ProviderExecution/SequentialProviderSearchExecutor.php",
            "src/Search/Application/Aggregator/SearchAggregator.php",
            "src/Search/Infrastructure/Plan/YamlSearchPlanParser.php",
            "tests/Unit/Search/SearchQueryTest.php",
            "tests/Unit/Search/Application/Aggregator/SearchAggregatorTest.php",
            "tests/Unit/Search/Application/ProviderExecution/SequentialProviderSearchExecutorTest.php",
            "tests/Unit/Search/Application/Plan/SearchPlanParserTest.php"
        }, ReadStrings(root.GetProperty("sourceRefs")));
        CollectionAssert.AreEqual(new[]
        {
            "PHP 8.3 or later",
            "git is available",
            "PHP reference tracked files are clean",
            "no network access or Composer dependencies are required",
            "future-year rejection uses a far-future value independent of wall-clock year",
            "runtime ids and durations are excluded from generated output",
            "UTF-8 JSON with LF line endings"
        }, ReadStrings(root.GetProperty("environmentAssumptions")));
        CollectionAssert.AreEqual(new[]
        {
            "generated query ids",
            "provider durations",
            "wall-clock max-year message text"
        }, ReadStrings(root.GetProperty("ignoredNondeterminism")));
        CollectionAssert.AreEqual(new[]
        {
            "compare normalized validation categories rather than language-specific exception classes",
            "compare cache field sensitivity and equality relations rather than cache-key byte equality",
            "compare provider registration and result order exactly",
            "exclude runtime durations and generated query ids",
            "require every case to have a reviewed semantic classification"
        }, ReadStrings(root.GetProperty("comparisonRules")));
        Assert.AreEqual(DigestFixture("input.json"), root.GetProperty("inputDigest").GetString());
        Assert.AreEqual(DigestFixture("expected.json"), root.GetProperty("outputDigest").GetString());
        Assert.AreEqual(DigestFile(SourceLockPath()), root.GetProperty("sourceLockDigest").GetString());
        Assert.AreEqual(DigestFixture("comparison.json"), root.GetProperty("classificationDigest").GetString());
    }

    [TestMethod]
    public void Every_php_case_has_one_resolved_reviewed_classification()
    {
        using var expected = Load("expected.json");
        using var comparison = Load("comparison.json");
        var expectedIds = expected.RootElement.GetProperty("cases").EnumerateArray()
            .Select(item => item.GetProperty("id").GetString()!).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var classifications = comparison.RootElement.GetProperty("classifications").EnumerateArray().ToArray();
        var classifiedIds = classifications.Select(item => item.GetProperty("caseId").GetString()!)
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(new[]
        {
            "cache-field-sensitivity",
            "cache-include-raw-data",
            "cache-provider-order",
            "legacy-plan-import",
            "provider-alias-normalization",
            "provider-all-failed",
            "provider-partial-failure",
            "provider-selection-all",
            "provider-selection-subset",
            "provider-selection-unknown",
            "query-term-short-rejected",
            "query-term-valid",
            "schema-closed-plan-unknown-field",
            "search-time-deduplication",
            "year-from-exceeds-validation-year",
            "year-range-inverted",
            "year-range-valid",
            "year-to-below-minimum"
        }, expectedIds);
        CollectionAssert.AreEqual(expectedIds, classifiedIds);
        Assert.AreEqual(classifiedIds.Length, classifiedIds.Distinct(StringComparer.Ordinal).Count());
        foreach (var classification in classifications)
        {
            var value = classification.GetProperty("classification").GetString();
            CollectionAssert.Contains(
                new[] { "equivalent_serialization", "intentional_change", "php_defect", "csharp_defect", "unresolved_specification_conflict" },
                value);
            Assert.AreNotEqual("csharp_defect", value, "H26 cannot close with a known C# defect.");
            Assert.AreNotEqual("unresolved_specification_conflict", value, "H26 cannot close with an unresolved specification conflict.");
            Assert.IsTrue(classification.GetProperty("authorityRefs").GetArrayLength() > 0);
        }
    }

    [TestMethod]
    public void Equivalent_php_cases_replay_with_matching_search_semantics()
    {
        using var input = Load("input.json");
        using var expected = Load("expected.json");
        using var comparison = Load("comparison.json");
        var validationYear = input.RootElement.GetProperty("validationYear").GetInt32();
        var inputs = input.RootElement.GetProperty("cases").EnumerateArray()
            .ToDictionary(item => item.GetProperty("id").GetString()!, item => item.Clone(), StringComparer.Ordinal);
        var outputs = expected.RootElement.GetProperty("cases").EnumerateArray()
            .ToDictionary(item => item.GetProperty("id").GetString()!, item => JsonNode.Parse(item.GetProperty("result").GetRawText())!, StringComparer.Ordinal);

        foreach (var classification in comparison.RootElement.GetProperty("classifications").EnumerateArray()
                     .Where(item => item.GetProperty("classification").GetString() == "equivalent_serialization"))
        {
            var caseId = classification.GetProperty("caseId").GetString()!;
            var rule = classification.TryGetProperty("comparisonRule", out var ruleElement) ? ruleElement.GetString()! : "exact";
            AssertEquivalent(caseId, rule, outputs[caseId], Replay(inputs[caseId], validationYear));
        }
    }

    [TestMethod]
    public void Intentional_changes_match_adr_0010_boundaries()
    {
        using var input = Load("input.json");
        using var expected = Load("expected.json");
        using var comparison = Load("comparison.json");
        var intentionalCaseIds = comparison.RootElement.GetProperty("classifications").EnumerateArray()
            .Where(item => item.GetProperty("classification").GetString() == "intentional_change")
            .Select(item => item.GetProperty("caseId").GetString()!)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(
            new[] { "cache-include-raw-data", "schema-closed-plan-unknown-field", "search-time-deduplication" },
            intentionalCaseIds);

        var validationYear = input.RootElement.GetProperty("validationYear").GetInt32();
        var cases = input.RootElement.GetProperty("cases").EnumerateArray()
            .ToDictionary(item => item.GetProperty("id").GetString()!, item => item.Clone(), StringComparer.Ordinal);

        var phpRawCache = Result(expected, "cache-include-raw-data");
        Assert.IsTrue(phpRawCache.GetProperty("equal").GetBoolean());
        var csharpRawCache = CacheIncludeRawData(cases["cache-include-raw-data"], validationYear);
        Assert.IsFalse(csharpRawCache["equal"]!.GetValue<bool>());

        Assert.IsTrue(Result(expected, "schema-closed-plan-unknown-field").GetProperty("accepted").GetBoolean());
        var planException = Assert.ThrowsExactly<SearchRuleException>(() =>
            SearchPlanParser.ParseSchemaClosed(cases["schema-closed-plan-unknown-field"].GetProperty("plan").GetRawText()));
        Assert.AreEqual(SearchErrorCodes.UnknownPlanRootField, planException.Category);

        var phpDedup = Result(expected, "search-time-deduplication");
        Assert.AreEqual(1, phpDedup.GetProperty("outputCount").GetInt32());
        var csharpTrace = ExecuteProviders(cases["search-time-deduplication"], validationYear);
        Assert.AreEqual(2, csharpTrace.Summary.RawSightingCount);
        Assert.AreEqual(2, csharpTrace.Sightings.Count);
    }

    private static JsonObject Replay(JsonElement fixtureCase, int validationYear)
    {
        return fixtureCase.GetProperty("operation").GetString() switch
        {
            "query-term" => QueryTerm(fixtureCase),
            "year-range" => YearRange(fixtureCase, validationYear),
            "provider-alias-normalization" => new JsonObject { ["aliases"] = StringArray(SearchService.NormalizeProviderAliases(ReadStrings(fixtureCase.GetProperty("aliases")))) },
            "cache-provider-order" => CacheProviderOrder(fixtureCase, validationYear),
            "cache-field-sensitivity" => CacheFieldSensitivity(fixtureCase, validationYear),
            "legacy-plan-import" => LegacyPlanImport(fixtureCase),
            "provider-selection" => ProviderSelection(fixtureCase, validationYear),
            "provider-execution" => ProviderExecution(fixtureCase, validationYear),
            _ => throw new AssertFailedException($"Unsupported equivalent operation '{fixtureCase.GetProperty("operation").GetString()}'.")
        };
    }

    private static JsonObject QueryTerm(JsonElement fixtureCase)
    {
        try
        {
            var term = SearchQueryTerm.From(fixtureCase.GetProperty("value").GetString()!);
            return new JsonObject { ["accepted"] = true, ["value"] = term.Value };
        }
        catch (SearchRuleException exception)
        {
            return new JsonObject { ["accepted"] = false, ["errorCategory"] = exception.Category };
        }
    }

    private static JsonObject YearRange(JsonElement fixtureCase, int validationYear)
    {
        try
        {
            var range = SearchYearRange.Validate(NullableInt(fixtureCase, "from"), NullableInt(fixtureCase, "to"), validationYear);
            return new JsonObject { ["accepted"] = true, ["from"] = range.From, ["to"] = range.To };
        }
        catch (SearchRuleException exception)
        {
            return new JsonObject { ["accepted"] = false, ["errorCategory"] = exception.Category };
        }
    }

    private static JsonObject CacheProviderOrder(JsonElement fixtureCase, int validationYear)
    {
        var input = ReadQueryInput(fixtureCase.GetProperty("request"));
        var first = SearchCacheIdentity.Compute(input, validationYear, ReadStrings(fixtureCase.GetProperty("providersA")));
        var second = SearchCacheIdentity.Compute(input, validationYear, ReadStrings(fixtureCase.GetProperty("providersB")));
        return new JsonObject { ["firstKey"] = first.CacheKey, ["secondKey"] = second.CacheKey, ["equal"] = first.CacheKey == second.CacheKey };
    }

    private static JsonObject CacheFieldSensitivity(JsonElement fixtureCase, int validationYear)
    {
        var request = fixtureCase.GetProperty("request");
        var providers = ReadStrings(fixtureCase.GetProperty("providers"));
        var input = ReadQueryInput(request);
        var baseKey = SearchCacheIdentity.Compute(input, validationYear, providers).CacheKey;
        string Key(SearchQueryInput changed, IReadOnlyList<string>? aliases = null) =>
            SearchCacheIdentity.Compute(changed, validationYear, aliases ?? providers).CacheKey;
        return new JsonObject
        {
            ["baseKey"] = baseKey,
            ["languageChangesKey"] = baseKey != Key(input with { Language = "fr" }),
            ["maxResultsChangesKey"] = baseKey != Key(input with { MaxResults = input.MaxResults + 1 }),
            ["offsetChangesKey"] = baseKey != Key(input with { Offset = input.Offset + 1 }),
            ["providersChangeKey"] = baseKey != Key(input, new[] { "crossref" })
        };
    }

    private static JsonObject CacheIncludeRawData(JsonElement fixtureCase, int validationYear)
    {
        var providers = ReadStrings(fixtureCase.GetProperty("providers"));
        var input = ReadQueryInput(fixtureCase.GetProperty("request"));
        var without = SearchCacheIdentity.Compute(input with { IncludeRawData = false }, validationYear, providers).CacheKey;
        var with = SearchCacheIdentity.Compute(input with { IncludeRawData = true }, validationYear, providers).CacheKey;
        return new JsonObject { ["withoutRawKey"] = without, ["withRawKey"] = with, ["equal"] = without == with };
    }

    private static JsonObject LegacyPlanImport(JsonElement fixtureCase)
    {
        var plan = SearchPlanParser.ParseLegacyImport(fixtureCase.GetProperty("plan").GetRawText());
        var items = new JsonArray(plan.Items.Select(item => (JsonNode)new JsonObject
        {
            ["id"] = item.ItemId,
            ["query"] = item.Query,
            ["projectId"] = item.ProjectId,
            ["maxResults"] = item.MaxResults,
            ["yearFrom"] = item.YearFrom,
            ["yearTo"] = item.YearTo,
            ["providerAliases"] = StringArray(item.Providers),
            ["includeRawData"] = item.IncludeRawData,
            ["sourceIndex"] = item.SourceIndex
        }).ToArray());
        return new JsonObject { ["projectId"] = plan.ProjectId, ["items"] = items };
    }

    private static JsonObject ProviderSelection(JsonElement fixtureCase, int validationYear)
    {
        var providers = ReadStrings(fixtureCase.GetProperty("registered"))
            .Select(alias => (ISearchProvider)new DataProvider(alias, _ => Array.Empty<ScholarlyWork>())).ToArray();
        try
        {
            var trace = new SearchService(providers).Execute(
                "trace-selection",
                new SearchQueryInput("search", null, null, null, 100, 0, false, ReadStrings(fixtureCase.GetProperty("selected"))),
                validationYear);
            return new JsonObject { ["accepted"] = true, ["aliases"] = StringArray(trace.ProviderAttempts.Select(item => item.ProviderAlias)) };
        }
        catch (SearchRuleException exception)
        {
            return new JsonObject { ["accepted"] = false, ["errorCategory"] = exception.Category };
        }
    }

    private static JsonObject ProviderExecution(JsonElement fixtureCase, int validationYear)
    {
        var trace = ExecuteProviders(fixtureCase, validationYear);
        var stats = new JsonArray(trace.ProviderAttempts.Select(attempt => (JsonNode)new JsonObject
        {
            ["alias"] = attempt.ProviderAlias,
            ["status"] = attempt.Status,
            ["resultCount"] = attempt.ResultCount,
            ["skipReason"] = attempt.SkipReason
        }).ToArray());
        return new JsonObject
        {
            ["stats"] = stats,
            ["workIds"] = StringArray(trace.Sightings.Select(sighting => sighting.ProviderWorkId!)),
            ["allFailed"] = trace.Summary.AllFailed
        };
    }

    private static SearchTrace ExecuteProviders(JsonElement fixtureCase, int validationYear)
    {
        var providers = fixtureCase.GetProperty("providers").EnumerateArray().Select(ReadProvider).ToArray();
        return new SearchService(providers).Execute(
            "trace-provider-execution",
            new SearchQueryInput("search", null, null, null, 100, 0, false, Array.Empty<string>()),
            validationYear);
    }

    private static ISearchProvider ReadProvider(JsonElement definition)
    {
        var alias = definition.GetProperty("alias").GetString()!;
        if (definition.TryGetProperty("failure", out var failure))
        {
            var message = failure.GetString()!;
            return new DataProvider(alias, _ => throw new InvalidOperationException(message));
        }

        var works = definition.GetProperty("works").EnumerateArray().Select(work =>
            ScholarlyWork.Identified(
                work.GetProperty("title").GetString()!,
                WorkIdSet.From(WorkId.From("doi", work.GetProperty("doi").GetString()!)),
                alias)).ToArray();
        return new DataProvider(alias, _ => works);
    }

    private static SearchQueryInput ReadQueryInput(JsonElement request) => new(
        request.GetProperty("query").GetString()!,
        NullableInt(request, "yearFrom"),
        NullableInt(request, "yearTo"),
        request.TryGetProperty("language", out var language) ? language.GetString() : null,
        request.TryGetProperty("maxResults", out var max) ? max.GetInt32() : 100,
        request.TryGetProperty("offset", out var offset) ? offset.GetInt32() : 0,
        request.TryGetProperty("includeRawData", out var raw) && raw.GetBoolean(),
        Array.Empty<string>());

    private static void AssertEquivalent(string caseId, string rule, JsonNode expected, JsonNode actual)
    {
        if (rule == "cache_relation")
        {
            foreach (var property in expected.AsObject().Where(pair => pair.Key.EndsWith("Key", StringComparison.Ordinal) || pair.Key == "equal"))
            {
                if (property.Value is JsonValue value && value.TryGetValue<bool>(out var expectedBoolean))
                {
                    Assert.AreEqual(expectedBoolean, actual[property.Key]!.GetValue<bool>(), $"{caseId}:{property.Key}");
                }
            }
            Assert.IsTrue(expected.AsObject().First(pair => pair.Key.EndsWith("Key", StringComparison.Ordinal)).Value!.GetValue<string>().Length == 64);
            Assert.IsTrue(actual.AsObject().First(pair => pair.Key.EndsWith("Key", StringComparison.Ordinal)).Value!.GetValue<string>().StartsWith("sha256:", StringComparison.Ordinal));
            return;
        }

        Assert.AreEqual("exact", rule, $"Unknown comparison rule for '{caseId}'.");
        Assert.IsTrue(JsonNode.DeepEquals(expected, actual), $"Semantic mismatch for '{caseId}'.\nExpected: {expected}\nActual: {actual}");
    }

    private static int? NullableInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetInt32() : null;

    private static string[] ReadStrings(JsonElement element) => element.EnumerateArray().Select(item => item.GetString()!).ToArray();

    private static JsonArray StringArray(IEnumerable<string> values) =>
        new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());

    private static JsonElement Result(JsonDocument document, string caseId) => document.RootElement.GetProperty("cases").EnumerateArray()
        .Single(item => item.GetProperty("id").GetString() == caseId).GetProperty("result");

    private static string DigestFixture(string fileName) => DigestFile(Path.Combine(FixtureDirectory(), fileName));
    private static string DigestFile(string path) => $"sha256:{Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)))}";
    private static JsonDocument Load(string fileName) => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(FixtureDirectory(), fileName)));
    private static string FixtureDirectory() => Path.Combine(AppContext.BaseDirectory, "fixtures", "php-golden", "search", "v1");
    private static string SourceLockPath() => Path.Combine(AppContext.BaseDirectory, "fixtures", "php-golden", "SOURCE.lock.json");
}
