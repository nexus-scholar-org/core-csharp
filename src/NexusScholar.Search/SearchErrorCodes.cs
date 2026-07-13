namespace NexusScholar.Search;

public static class SearchErrorCodes
{
    public const string InvalidSearchTerm = "invalid-search-term";
    public const string QueryLengthBelowMinimum = "query-length-below-minimum";
    public const string YearFromBelowMinimum = "year-from-below-minimum";
    public const string YearToBelowMinimum = "year-to-below-minimum";
    public const string YearFromExceedsValidationYear = "year-from-exceeds-validation-year";
    public const string YearToExceedsValidationYear = "year-to-exceeds-validation-year";
    public const string YearRangeInverted = "year-range-inverted";
    public const string NonPositiveMaxResults = "non-positive-max-results";
    public const string UnknownProviderAlias = "unknown-provider-alias";
    public const string UnknownPlanSchemaId = "unknown-plan-schema-id";
    public const string UnsupportedPlanSchemaVersion = "unsupported-plan-schema-version";
    public const string MissingPlanSchemaId = "missing-plan-schema-id";
    public const string MissingPlanSchemaVersion = "missing-plan-schema-version";
    public const string MissingPlanSearches = "missing-plan-searches";
    public const string InvalidPlanItemShape = "invalid-plan-item-shape";
    public const string UnknownPlanRootField = "unknown-plan-root-field";
    public const string UnknownPlanItemField = "unknown-plan-item-field";
    public const string ProviderExecutionFailed = "provider-execution-failed";

    public const string KnownPlanSchemaId = "nexus.search.plan";
    public const string KnownPlanSchemaVersion = "1.0.0";

    public static readonly string[] KnownPlanRootFields =
    [
        "schema_id",
        "schema_version",
        "project_id",
        "max_results",
        "language",
        "providers",
        "include_raw_data",
        "searches"
    ];

    public static readonly string[] KnownPlanItemFields =
    [
        "id",
        "label",
        "query",
        "project_id",
        "limit",
        "max_results",
        "year_from",
        "year_to",
        "providers",
        "include_raw_data",
        "metadata"
    ];
}
