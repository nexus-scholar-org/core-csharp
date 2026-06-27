using NexusScholar.Kernel;

namespace NexusScholar.Search;

public sealed record SearchPlan(
    string SchemaId,
    string SchemaVersion,
    string ProjectId,
    int DefaultMaxResults,
    string? Language,
    IReadOnlyList<string> DefaultProviderAliases,
    bool DefaultIncludeRawData,
    IReadOnlyList<SearchPlanItem> Items,
    SearchPlanSource Source);

public sealed record SearchPlanItem(
    string ItemId,
    string? Label,
    string Query,
    string ProjectId,
    int MaxResults,
    int? YearFrom,
    int? YearTo,
    IReadOnlyList<string> Providers,
    bool IncludeRawData,
    int SourceIndex);

public enum SearchPlanSource
{
    SchemaClosed,
    PhpLegacyImport
}

public sealed record ParsedSearchPlan(
    string SchemaId,
    string SchemaVersion,
    string ProjectId,
    int MaxResults,
    bool IncludeRawData,
    string? Language,
    IReadOnlyList<string> Providers,
    IReadOnlyList<SearchPlanItem> Items,
    SearchPlanSource Source);
