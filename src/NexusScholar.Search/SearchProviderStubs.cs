using System.Collections.ObjectModel;
using NexusScholar.Shared;

namespace NexusScholar.Search;

public sealed record SearchProviderExecutionContext(
    string Query,
    SearchYearRange? YearRange,
    string? Language,
    int MaxResults,
    int Offset,
    bool IncludeRawData);

public interface ISearchProvider
{
    string Alias { get; }

    IReadOnlyList<ScholarlyWork> Execute(SearchProviderExecutionContext context);
}

public sealed class SearchProviderCatalog
{
    private static readonly IReadOnlyList<string> DefaultAliases =
        new[]
        {
            "openalex",
            "crossref",
            "semantic_scholar",
            "arxiv",
            "pubmed",
            "ieee",
            "doaj"
        };

    public static IReadOnlyList<ISearchProvider> DefaultProviders()
    {
        return new ReadOnlyCollection<ISearchProvider>(DefaultAliases.Select(CreateProvider).ToArray());
    }

    private static ISearchProvider CreateProvider(string alias) =>
        alias switch
        {
            "openalex" => new DataProvider(alias, BuildOpenAlexWorks),
            "crossref" => new DataProvider(alias, BuildCrossrefWorks),
            "semantic_scholar" => new DataProvider(alias, BuildSemanticScholarWorks),
            "arxiv" => new DataProvider(alias, BuildArxivWorks),
            "pubmed" => new DataProvider(alias, BuildPubmedWorks),
            "ieee" => new FailingProvider(alias, "Provider unavailable"),
            "doaj" => new FailingProvider(alias, "Provider unavailable"),
            _ => throw new InvalidOperationException($"Unsupported provider alias '{alias}'.")
        };

    private static IReadOnlyList<ScholarlyWork> BuildOpenAlexWorks(SearchProviderExecutionContext context)
    {
        return new[]
        {
            ScholarlyWork.Identified(
                "OpenAlex Seed",
                WorkIdSet.From(
                    WorkId.From("openalex", "W001"),
                    WorkId.From("doi", "10.1000/seed")),
                sourceContext: "openalex:W001",
                rawData: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["raw_provider_payload"] = "openalex-raw"
                }),
            ScholarlyWork.Identified(
                "OpenAlex Shared",
                WorkIdSet.From(WorkId.From("doi", "10.1000/shared")),
                sourceContext: "openalex:shared",
                rawData: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["raw_provider_payload"] = "openalex-shared-raw"
                })
        };
    }

    private static IReadOnlyList<ScholarlyWork> BuildCrossrefWorks(SearchProviderExecutionContext context)
    {
        return new[]
        {
            ScholarlyWork.Identified(
                "Crossref Shared",
                WorkIdSet.From(WorkId.From("doi", "10.1000/shared")),
                sourceContext: "crossref:shared",
                rawData: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["raw_provider_payload"] = "crossref-shared-raw"
                }),
            ScholarlyWork.Identified(
                "Crossref Unique",
                WorkIdSet.From(WorkId.From("doi", "10.1000/cross-2")),
                sourceContext: "crossref:cross-2",
                rawData: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["raw_provider_payload"] = "crossref-unique-raw"
                })
        };
    }

    private static IReadOnlyList<ScholarlyWork> BuildSemanticScholarWorks(SearchProviderExecutionContext context)
    {
        return new[]
        {
            ScholarlyWork.Identified(
                "Scholar Supplemental",
                WorkIdSet.From(WorkId.From("s2", "S2-9")),
                sourceContext: "semantic_scholar:s2-9",
                rawData: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["raw_provider_payload"] = "semantic-s2"
                })
        };
    }

    private static IReadOnlyList<ScholarlyWork> BuildArxivWorks(SearchProviderExecutionContext context)
    {
        return new[]
        {
            ScholarlyWork.UnresolvedCandidate(
                "No stable id arXiv",
                "arxiv:row-1",
                rawData: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["raw_provider_payload"] = "arxiv-no-id-raw"
                })
        };
    }

    private static IReadOnlyList<ScholarlyWork> BuildPubmedWorks(SearchProviderExecutionContext context)
    {
        return new[]
        {
            ScholarlyWork.Identified(
                "PubMed Unique",
                WorkIdSet.From(WorkId.From("pubmed", "PM123")),
                sourceContext: "pubmed:pm123",
                rawData: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["raw_provider_payload"] = "pubmed-payload"
                })
        };
    }

}

public sealed class DataProvider : ISearchProvider
{
    private readonly Func<SearchProviderExecutionContext, IReadOnlyList<ScholarlyWork>> _executor;

    public DataProvider(string alias, Func<SearchProviderExecutionContext, IReadOnlyList<ScholarlyWork>> executor)
    {
        Alias = alias;
        _executor = executor;
    }

    public string Alias { get; }

    public IReadOnlyList<ScholarlyWork> Execute(SearchProviderExecutionContext context)
    {
        return _executor(context);
    }
}

public sealed class FailingProvider : ISearchProvider
{
    public FailingProvider(string alias, string reason)
    {
        Alias = alias;
        _reason = reason;
    }

    public string Alias { get; }

    public IReadOnlyList<ScholarlyWork> Execute(SearchProviderExecutionContext context)
    {
        throw new SearchRuleException(SearchErrorCodes.ProviderExecutionFailed, _reason);
    }

    private readonly string _reason;
}
