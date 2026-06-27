using System.Collections.ObjectModel;
using NexusScholar.Kernel;
using NexusScholar.Shared;

namespace NexusScholar.Search;

public sealed record SearchQueryInput(
    string Query,
    int? YearFrom,
    int? YearTo,
    string? Language,
    int MaxResults,
    int Offset,
    bool IncludeRawData,
    IReadOnlyList<string> SelectedProviderAliases,
    SearchPlanBinding? PlanBinding = null);

public sealed record SearchPlanBinding(
    string PlanId,
    string ItemId,
    string SchemaId,
    string SchemaVersion,
    string? ProjectId = null);

public sealed record SearchYearRange(int? From, int? To)
{
    public static SearchYearRange Validate(int? from, int? to, int validationYear)
    {
        var maxYear = validationYear + 5;

        if (from.HasValue && from.Value < 1000)
        {
            throw new SearchRuleException(
                SearchErrorCodes.YearFromBelowMinimum,
                "Search year_from must be 1000 or greater.");
        }

        if (to.HasValue && to.Value > maxYear)
        {
            throw new SearchRuleException(
                SearchErrorCodes.YearToExceedsValidationYear,
                "Search year_to exceeds validationYear + 5.");
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new SearchRuleException(
                SearchErrorCodes.YearRangeInverted,
                "Search year range is inverted.");
        }

        return new SearchYearRange(from, to);
    }
}

public sealed record SearchQueryTerm(string Value)
{
    public static SearchQueryTerm From(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length < 2)
        {
            throw new SearchRuleException(
                SearchErrorCodes.QueryLengthBelowMinimum,
                "Search term requires at least two non-whitespace characters.");
        }

        return new SearchQueryTerm(trimmed);
    }
}

public sealed record SearchTraceRequest(
    string Query,
    SearchYearRange? YearRange,
    string? Language,
    int MaxResults,
    int Offset,
    bool IncludeRawData,
    IReadOnlyList<string> SelectedProviderAliases,
    IReadOnlyList<string> ActiveProviderAliases,
    SearchPlanBinding? PlanBinding = null);

public sealed record SearchCacheIdentity(
    string Algorithm,
    string MaterialVersion,
    IReadOnlyList<string> IncludedFields,
    IReadOnlyList<string> ExcludedFields,
    bool ProviderOrderInsensitive,
    string CacheKey,
    string TraceMaterial)
{
    public const string AlgorithmId = "sha256";
    public const string MaterialVersionId = "1.0.0";

    public static readonly IReadOnlyList<string> IncludedFieldNames =
        new ReadOnlyCollection<string>(
            [
                "query",
                "year_from",
                "year_to",
                "language",
                "max_results",
                "offset",
                "active_provider_aliases",
                "include_raw_data"
            ]);

    public static readonly IReadOnlyList<string> ExcludedFieldNames =
        new ReadOnlyCollection<string>(
            [
                "query_id",
                "trace_id",
                "project_id",
                "runtime_duration_ms",
                "provider_stats",
                "provider_failures",
                "raw_payload_bytes",
                "app_id",
                "app_hash",
                "local_paths",
                "provider_credentials"
            ]);

    public static SearchCacheIdentity Compute(
        SearchQueryInput input,
        int validationYear,
        IReadOnlyList<string> activeAliases)
    {
        ArgumentNullException.ThrowIfNull(input);
        _ = validationYear;

        var query = SearchQueryTerm.From(input.Query);
        var yearRange = SearchYearRange.Validate(input.YearFrom, input.YearTo, validationYear);

        var normalizedAliases = SearchService.NormalizeProviderAliases(activeAliases).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var material = new CanonicalJsonObject()
            .Add("query", query.Value)
            .Add("year_from", yearRange.From?.ToString() ?? string.Empty)
            .Add("year_to", yearRange.To?.ToString() ?? string.Empty)
            .Add("language", input.Language ?? string.Empty)
            .Add("max_results", input.MaxResults)
            .Add("offset", input.Offset)
            .Add(
                "active_provider_aliases",
                CanonicalJsonValue.Array(normalizedAliases.Select(CanonicalJsonValue.From).ToArray()))
            .Add("include_raw_data", input.IncludeRawData);

        var key = ContentDigest.Sha256CanonicalJson(material);
        return new SearchCacheIdentity(
            AlgorithmId,
            MaterialVersionId,
            IncludedFieldNames,
            ExcludedFieldNames,
            ProviderOrderInsensitive: true,
            key.ToString(),
            CanonicalJsonSerializer.Serialize(material));
    }
}

public sealed record SearchProviderAttempt(int AttemptOrder, string ProviderAlias, string Status, int ResultCount, string? SkipReason = null);

public sealed record SearchProviderStat(string ProviderAlias, int ResultCount, long DurationMs, string? SkipReason = null);

public sealed record SearchSummary(
    int AttemptedProviders,
    int SucceededProviders,
    int FailedProviders,
    int RawSightingCount,
    bool AllFailed);

public sealed record SearchSighting(
    string ProviderAlias,
    int ProviderOrder,
    int ProviderLocalRank,
    ScholarlyWork Work)
{
    public string? ProviderWorkId => Work.PrimaryWorkId?.ToString();
    public IReadOnlyList<string> WorkIds => Work.WorkIds.Ids.Select(identifier => identifier.ToString()).ToArray();
}

public sealed record SearchTrace(
    string TraceId,
    string SchemaId,
    string SchemaVersion,
    SearchTraceRequest Request,
    SearchCacheIdentity CacheIdentity,
    IReadOnlyList<SearchProviderAttempt> ProviderAttempts,
    IReadOnlyList<SearchProviderStat> ProviderStats,
    IReadOnlyList<SearchSighting> Sightings,
    SearchSummary Summary,
    IReadOnlyList<string> NonClaims)
{
    public const string TraceSchemaId = "nexus.search.trace";
    public const string TraceSchemaVersion = "1.0.0";

    public static readonly IReadOnlyList<string> DefaultNonClaims = new[]
    {
        "no-php-compatibility-claim",
        "no-live-provider-network",
        "no-import-parser-implementation",
        "no-dedup-at-search-time"
    };
}
