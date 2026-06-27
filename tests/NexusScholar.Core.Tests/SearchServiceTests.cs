using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Search;

namespace NexusScholar.Core.Tests;

[TestClass]
public sealed class SearchServiceTests
{
    private const int ValidationYear = 2026;

    [TestMethod]
    public void Search_term_validation_rejects_short_queries()
    {
        var service = NewService();
        var input = new SearchQueryInput("a", 2020, null, null, 10, 0, false, Array.Empty<string>());
        Assert.ThrowsExactly<SearchRuleException>(() => service.Execute("trace-1", input, ValidationYear));
    }

    [TestMethod]
    public void Search_year_validation_rejects_invalid_ranges()
    {
        var service = NewService();
        Assert.ThrowsExactly<SearchRuleException>(() =>
            service.Execute("trace-1", new SearchQueryInput("alpha", 999, 2000, null, 10, 0, false, Array.Empty<string>()), ValidationYear));

        Assert.ThrowsExactly<SearchRuleException>(() =>
            service.Execute("trace-1", new SearchQueryInput("alpha", null, 2035, null, 10, 0, false, Array.Empty<string>()), ValidationYear));

        Assert.ThrowsExactly<SearchRuleException>(() =>
            service.Execute("trace-1", new SearchQueryInput("alpha", 2020, 2019, null, 10, 0, false, Array.Empty<string>()), ValidationYear));
    }

    [TestMethod]
    public void Search_year_validation_uses_injected_clock_year_not_wall_clock()
    {
        var service = NewService();
        var input = new SearchQueryInput("alpha", null, 2032, null, 10, 0, false, Array.Empty<string>());
        Assert.ThrowsExactly<SearchRuleException>(() => service.Execute("trace-1", input, ValidationYear));
        var trace = service.Execute("trace-2", input, 2035);
        Assert.AreEqual(2032, trace.Request.YearRange!.To);
    }

    [TestMethod]
    public void Search_provider_aliases_are_normalized_and_deduplicated()
    {
        var normalized = SearchService.NormalizeProviderAliases(
            new[]
        {
            " OpenAlex ",
            "",
            "openalex",
            "CROSSREF",
            "crossref ",
            "  "
        });

        CollectionAssert.AreEqual(new[] { "openalex", "crossref" }, normalized.ToArray());
    }

    [TestMethod]
    public void Search_cache_identity_is_provider_order_insensitive()
    {
        var input = new SearchQueryInput("seed", 2020, 2021, "en", 50, 0, false, Array.Empty<string>());
        var defaultProviders = new[] { "crossref", "openalex", "semantic_scholar" };
        var swappedProviders = new[] { "semantic_scholar", "openalex", "crossref" };
        var first = SearchCacheIdentity.Compute(input, ValidationYear, defaultProviders);
        var second = SearchCacheIdentity.Compute(input, ValidationYear, swappedProviders);

        Assert.AreEqual(first.CacheKey, second.CacheKey);
        CollectionAssert.AreEqual(first.IncludedFields.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            second.IncludedFields.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.IsFalse(first.IncludedFields.Any(field => string.Equals(field, "query_id", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Search_execution_rejects_unknown_alias_before_execution_and_cache_lookup()
    {
        var service = NewService();
        var input = new SearchQueryInput("alpha", null, null, null, 10, 0, false, new[] { "unknown", "openalex" });
        var exception = Assert.ThrowsExactly<SearchRuleException>(() => service.Execute("trace-1", input, ValidationYear));
        Assert.AreEqual(SearchErrorCodes.UnknownProviderAlias, exception.Category);
    }

    [TestMethod]
    public void Search_execution_uses_registration_order_for_subset_selection()
    {
        var service = NewService();
        var input = new SearchQueryInput(
            "alpha",
            null,
            null,
            null,
            10,
            0,
            false,
            new[] { "crossref", "openalex" });

        var trace = service.Execute("trace-1", input, ValidationYear);
        var attempts = trace.ProviderAttempts.Select(attempt => attempt.ProviderAlias).ToArray();
        CollectionAssert.AreEqual(new[] { "openalex", "crossref" }, attempts);
        Assert.IsTrue(attempts.Length > 0);
    }

    [TestMethod]
    public void Search_execution_supports_empty_provider_selection_as_all_active_providers()
    {
        var service = NewService();
        var input = new SearchQueryInput("alpha", null, null, null, 10, 0, false, Array.Empty<string>());
        var trace = service.Execute("trace-1", input, ValidationYear);

        var expectedAliases = SearchProviderCatalog.DefaultProviders().Select(provider => provider.Alias).ToList();
        CollectionAssert.AreEqual(expectedAliases.ToArray(), trace.ProviderAttempts.Select(attempt => attempt.ProviderAlias).ToArray());
    }

    [TestMethod]
    public void Search_execution_preserves_duplicate_provider_sightings()
    {
        var service = NewService();
        var input = new SearchQueryInput("alpha", null, null, null, 10, 0, false, new[] { "openalex", "crossref" });
        var trace = service.Execute("trace-1", input, ValidationYear);

        Assert.IsTrue(trace.Sightings.Count(s => s.ProviderWorkId == "doi:10.1000/shared") >= 2);
        Assert.AreEqual(trace.Sightings.Count, trace.ProviderAttempts.Sum(attempt => attempt.ResultCount));
    }

    [TestMethod]
    public void Search_execution_applies_max_results_and_offset_per_provider()
    {
        var service = NewService();
        var input = new SearchQueryInput("alpha", null, null, null, 1, 1, false, new[] { "openalex" });
        var trace = service.Execute("trace-1", input, ValidationYear);

        Assert.AreEqual(1, trace.Sightings.Count);
        Assert.AreEqual("openalex:shared", trace.Sightings[0].Work.SourceContext);
        Assert.AreEqual(1, trace.ProviderAttempts[0].ResultCount);
    }

    [TestMethod]
    public void Search_trace_collections_are_read_only_snapshots()
    {
        var service = NewService();
        var input = new SearchQueryInput("alpha", null, null, null, 10, 0, false, new[] { "openalex" });
        var trace = service.Execute("trace-1", input, ValidationYear);

        var attempts = (IList<SearchProviderAttempt>)trace.ProviderAttempts;
        var stats = (IList<SearchProviderStat>)trace.ProviderStats;
        var sightings = (IList<SearchSighting>)trace.Sightings;

        Assert.ThrowsExactly<NotSupportedException>(() => attempts.Add(trace.ProviderAttempts[0]));
        Assert.ThrowsExactly<NotSupportedException>(() => stats.Add(trace.ProviderStats[0]));
        Assert.ThrowsExactly<NotSupportedException>(() => sightings.Add(trace.Sightings[0]));
    }

    [TestMethod]
    public void Search_execution_preserves_no_id_candidates_as_unresolved()
    {
        var service = NewService();
        var input = new SearchQueryInput("alpha", null, null, null, 10, 0, false, new[] { "arxiv" });
        var trace = service.Execute("trace-1", input, ValidationYear);

        Assert.AreEqual(1, trace.Sightings.Count);
        Assert.IsNull(trace.Sightings[0].ProviderWorkId);
        Assert.AreEqual("arxiv:row-1", trace.Sightings[0].Work.SourceContext);
        Assert.IsTrue(trace.Summary.RawSightingCount > 0);
    }

    [TestMethod]
    public void Search_execution_records_partial_and_all_failed_results()
    {
        var service = NewService();
        var partial = service.Execute("trace-1", new SearchQueryInput("alpha", null, null, null, 10, 0, false, new[] { "openalex", "ieee" }), ValidationYear);
        Assert.AreEqual(2, partial.ProviderAttempts.Count);
        Assert.AreEqual(1, partial.ProviderAttempts.Count(attempt => attempt.Status == "success"));
        Assert.AreEqual(1, partial.ProviderAttempts.Count(attempt => attempt.Status == "failure"));
        Assert.IsFalse(partial.Summary.AllFailed);

        var allFailed = service.Execute("trace-2", new SearchQueryInput("alpha", null, null, null, 10, 0, false, new[] { "ieee", "doaj" }), ValidationYear);
        Assert.IsTrue(allFailed.Summary.AllFailed);
        Assert.AreEqual(0, allFailed.Sightings.Count);
    }

    [TestMethod]
    public void Search_trace_exposes_cache_identity_and_raw_fields()
    {
        var service = NewService();
        var input = new SearchQueryInput("alpha", 2020, 2025, "en", 10, 0, true, new[] { "openalex", "crossref" });
        var trace = service.Execute("trace-1", input, ValidationYear);

        Assert.AreEqual(SearchTrace.TraceSchemaId, trace.SchemaId);
        Assert.AreEqual(SearchTrace.TraceSchemaVersion, trace.SchemaVersion);
        Assert.AreEqual("alpha", trace.Request.Query);
        Assert.AreEqual("openalex", trace.ProviderAttempts[0].ProviderAlias);
        Assert.AreEqual("crossref", trace.ProviderAttempts[1].ProviderAlias);
        Assert.AreEqual("query", trace.CacheIdentity.IncludedFields[0]);
        Assert.AreEqual("1.0.0", trace.CacheIdentity.MaterialVersion);
        Assert.AreEqual(SearchCacheIdentity.AlgorithmId, trace.CacheIdentity.Algorithm);
        Assert.IsTrue(trace.ProviderStats[0].DurationMs >= 0);
        Assert.IsTrue(trace.CacheIdentity.ExcludedFields.Contains("provider_stats"));
        Assert.IsFalse(trace.ProviderAttempts.All(attempt => attempt.Status == "failure"));
    }

    [TestMethod]
    public void Search_plan_parser_accepts_schema_closed_plan_and_rejects_unknown_fields()
    {
        var fixturePlan = @"{
            ""schema_id"":""nexus.search.plan"",
            ""schema_version"":""1.0.0"",
            ""project_id"":""project-1"",
            ""searches"": [
                {""id"":""search-1"",""query"":""alpha"",""max_results"":5}
            ]
        }";

        var parsed = SearchPlanParser.ParseSchemaClosed(fixturePlan);
        Assert.AreEqual(SearchErrorCodes.KnownPlanSchemaId, parsed.SchemaId);
        Assert.AreEqual(SearchErrorCodes.KnownPlanSchemaVersion, parsed.SchemaVersion);
        Assert.AreEqual(SearchPlanSource.SchemaClosed, parsed.Source);

        var legacyPlanText = @"{
            ""project"":""project-1"",
            ""queries"": [
                {""text"":""alpha"", ""year_min"":2020, ""year_max"":2022, ""limit"":3}
            ]
        }";
        var legacy = SearchPlanParser.ParseLegacyImport(legacyPlanText);
        Assert.AreEqual(SearchPlanSource.PhpLegacyImport, legacy.Source);

        var unknownFieldPlan = @"{
            ""schema_id"":""nexus.search.plan"",
            ""schema_version"":""1.0.0"",
            ""bad_unknown"":true,
            ""searches"": [
                {""id"":""search-1"",""query"":""alpha""}
            ]
        }";
        var unknownField = Assert.ThrowsExactly<SearchRuleException>(() => SearchPlanParser.ParseSchemaClosed(unknownFieldPlan));
        Assert.AreEqual(SearchErrorCodes.UnknownPlanRootField, unknownField.Category);

        var missingSchemaVersion = @"{
            ""schema_id"":""nexus.search.plan"",
            ""searches"": [
                {""id"":""search-1"",""query"":""alpha""}
            ]
        }";
        var missingSchema = Assert.ThrowsExactly<SearchRuleException>(() => SearchPlanParser.ParseSchemaClosed(missingSchemaVersion));
        Assert.AreEqual(SearchErrorCodes.MissingPlanSchemaVersion, missingSchema.Category);
    }

    [TestMethod]
    public void Search_plan_parser_rejects_unsupported_schema_version()
    {
        var unsupported = @"{
            ""schema_id"":""nexus.search.plan"",
            ""schema_version"":""2.0.0"",
            ""searches"": [
                {""id"":""search-1"",""query"":""alpha""}
            ]
        }";
        var exception = Assert.ThrowsExactly<SearchRuleException>(() => SearchPlanParser.ParseSchemaClosed(unsupported));
        Assert.AreEqual(SearchErrorCodes.UnsupportedPlanSchemaVersion, exception.Category);
    }

    private static SearchService NewService() => new SearchService(SearchProviderCatalog.DefaultProviders());
}
