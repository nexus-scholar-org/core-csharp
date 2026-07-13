using System.Collections.ObjectModel;
using System.Diagnostics;
using NexusScholar.Kernel;

namespace NexusScholar.Search;

public sealed class SearchService
{
    private readonly IReadOnlyList<ISearchProvider> _providers;
    private readonly Dictionary<string, ISearchProvider> _providerMap;

    public SearchService(IReadOnlyList<ISearchProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        if (providers.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(providers), "At least one provider is required.");
        }

        _providers = providers;
        _providerMap = providers.ToDictionary(provider => provider.Alias, StringComparer.OrdinalIgnoreCase);
    }

    public SearchTrace Execute(string traceId, SearchQueryInput input, int validationYear)
    {
        ArgumentNullException.ThrowIfNull(traceId);
        ArgumentNullException.ThrowIfNull(input);

        var queryTerm = SearchQueryTerm.From(input.Query);
        if (input.MaxResults <= 0)
        {
            throw new SearchRuleException(SearchErrorCodes.NonPositiveMaxResults, "max_results must be positive.");
        }

        if (input.Offset < 0)
        {
            throw new SearchRuleException(SearchErrorCodes.InvalidPlanItemShape, "offset cannot be negative.");
        }

        var yearRange = SearchYearRange.Validate(input.YearFrom, input.YearTo, validationYear);
        var selectedAliases = NormalizeProviderAliases(input.SelectedProviderAliases);
        var activeAliases = ActiveProviderAliases();
        var resolvedAliases = ResolveExecutionAliases(activeAliases, selectedAliases);

        var request = new SearchTraceRequest(
            queryTerm.Value,
            yearRange,
            input.Language,
            input.MaxResults,
            input.Offset,
            input.IncludeRawData,
            selectedAliases,
            resolvedAliases,
            input.PlanBinding);

        var cacheIdentity = SearchCacheIdentity.Compute(input, validationYear, resolvedAliases);

        var providerAttempts = new List<SearchProviderAttempt>();
        var providerStats = new List<SearchProviderStat>();
        var sightings = new List<SearchSighting>();

        var executionContext = new SearchProviderExecutionContext(
            queryTerm.Value,
            yearRange,
            input.Language,
            input.MaxResults,
            input.Offset,
            input.IncludeRawData);

        var providerAttemptOrder = 1;
        var providerOrder = 1;
        foreach (var alias in resolvedAliases)
        {
            var stopwatch = Stopwatch.StartNew();
            var provider = _providerMap[alias];
            IReadOnlyList<NexusScholar.Shared.ScholarlyWork> providerWorks;
            try
            {
                providerWorks = provider.Execute(executionContext);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                providerAttempts.Add(new SearchProviderAttempt(providerAttemptOrder, alias, "failure", 0, ex.Message));
                providerStats.Add(new SearchProviderStat(alias, 0, stopwatch.ElapsedMilliseconds, ex.Message));
                providerAttemptOrder++;
                providerOrder++;
                continue;
            }

            var rawWorks = providerWorks
                .Skip(input.Offset)
                .Take(input.MaxResults)
                .ToArray();
            stopwatch.Stop();

            var normalizedWorks = rawWorks
                .Select(work => input.IncludeRawData ? work : work.WithoutRawData())
                .ToArray();

            var providerRank = 1;
            foreach (var work in normalizedWorks)
            {
                sightings.Add(new SearchSighting(alias, providerOrder, providerRank++, work));
            }

            providerAttempts.Add(new SearchProviderAttempt(providerAttemptOrder, alias, "success", normalizedWorks.Length, null));
            providerStats.Add(new SearchProviderStat(alias, normalizedWorks.Length, stopwatch.ElapsedMilliseconds, null));
            providerAttemptOrder++;
            providerOrder++;
        }

        var summary = new SearchSummary(
            providerAttempts.Count,
            providerAttempts.Count(attempt => string.Equals(attempt.Status, "success", StringComparison.Ordinal)),
            providerAttempts.Count(attempt => string.Equals(attempt.Status, "failure", StringComparison.Ordinal)),
            sightings.Count,
            providerAttempts.Count > 0 && providerAttempts.All(attempt => string.Equals(attempt.Status, "failure", StringComparison.Ordinal)));

        return new SearchTrace(
            traceId,
            SearchTrace.TraceSchemaId,
            SearchTrace.TraceSchemaVersion,
            request,
            cacheIdentity,
            new ReadOnlyCollection<SearchProviderAttempt>(providerAttempts.ToArray()),
            new ReadOnlyCollection<SearchProviderStat>(providerStats.ToArray()),
            new ReadOnlyCollection<SearchSighting>(sightings.ToArray()),
            summary,
            SearchTrace.DefaultNonClaims);
    }

    private IReadOnlyList<string> ResolveExecutionAliases(
        IReadOnlyList<string> activeAliases,
        IReadOnlyList<string> selectedAliases)
    {
        if (selectedAliases.Count == 0)
        {
            return new ReadOnlyCollection<string>(activeAliases.ToArray());
        }

        var activeSet = new HashSet<string>(activeAliases, StringComparer.Ordinal);
        var requestedSet = selectedAliases.ToHashSet(StringComparer.Ordinal);
        foreach (var alias in requestedSet)
        {
            if (!activeSet.Contains(alias))
            {
                throw new SearchRuleException(
                    SearchErrorCodes.UnknownProviderAlias,
                    $"Unknown provider alias '{alias}'.");
            }
        }

        return new ReadOnlyCollection<string>(activeAliases.Where(alias => requestedSet.Contains(alias)).ToArray());
    }

    private IReadOnlyList<string> ActiveProviderAliases()
    {
        return new ReadOnlyCollection<string>(_providers.Select(provider => provider.Alias.ToLowerInvariant()).ToArray());
    }

    public static IReadOnlyList<string> NormalizeProviderAliases(IEnumerable<string> aliases)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<string>();

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                continue;
            }

            var normalizedAlias = alias.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedAlias))
            {
                continue;
            }

            if (seen.Add(normalizedAlias))
            {
                normalized.Add(normalizedAlias);
            }
        }

        return new ReadOnlyCollection<string>(normalized.ToArray());
    }
}
