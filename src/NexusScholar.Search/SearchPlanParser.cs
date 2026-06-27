using System.Collections.ObjectModel;
using System.Text.Json;
using NexusScholar.Kernel;

namespace NexusScholar.Search;

public static class SearchPlanParser
{
    public static ParsedSearchPlan ParseSchemaClosed(string jsonText)
    {
        using var document = JsonDocument.Parse(jsonText);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new SearchRuleException(SearchErrorCodes.InvalidPlanItemShape, "Search plan payload must be a JSON object.");
        }

        EnsureKnownRootFields(root);

        if (!root.TryGetProperty("schema_id", out var schemaIdElement))
        {
            throw new SearchRuleException(SearchErrorCodes.MissingPlanSchemaId, "Missing schema_id.");
        }

        if (!root.TryGetProperty("schema_version", out var schemaVersionElement))
        {
            throw new SearchRuleException(SearchErrorCodes.MissingPlanSchemaVersion, "Missing schema_version.");
        }

        var schemaId = Guard.NotBlank(schemaIdElement.GetString(), "schema_id");
        var schemaVersion = Guard.NotBlank(schemaVersionElement.GetString(), "schema_version");

        if (!string.Equals(schemaId, SearchErrorCodes.KnownPlanSchemaId, StringComparison.Ordinal))
        {
            throw new SearchRuleException(SearchErrorCodes.UnknownPlanSchemaId, $"Unknown schema id '{schemaId}'.");
        }

        if (!string.Equals(schemaVersion, SearchErrorCodes.KnownPlanSchemaVersion, StringComparison.Ordinal))
        {
            throw new SearchRuleException(SearchErrorCodes.UnsupportedPlanSchemaVersion, $"Unsupported schema version '{schemaVersion}'.");
        }

        var projectId = root.TryGetProperty("project_id", out var projectIdElement)
            ? Guard.NotBlank(projectIdElement.GetString(), "project_id")
            : "default-project";

        var defaultMaxResults = root.TryGetProperty("max_results", out var maxElement) ? maxElement.GetInt32() : 50;
        if (defaultMaxResults <= 0)
        {
            throw new SearchRuleException(SearchErrorCodes.NonPositiveMaxResults, "max_results must be positive.");
        }

        var language = root.TryGetProperty("language", out var languageElement)
            ? languageElement.GetString()
            : null;

        var defaultProviders = root.TryGetProperty("providers", out var providersElement)
            ? ParseAliasList(providersElement, SearchErrorCodes.UnknownPlanItemField)
            : Array.Empty<string>();

        if (!root.TryGetProperty("searches", out var itemsElement))
        {
            throw new SearchRuleException(SearchErrorCodes.MissingPlanSearches, "Search plan requires a searches array.");
        }

        if (itemsElement.ValueKind != JsonValueKind.Array)
        {
            throw new SearchRuleException(SearchErrorCodes.InvalidPlanItemShape, "searches must be an array.");
        }

        var items = new List<SearchPlanItem>();
        var sourceIndex = 0;
        foreach (var itemElement in itemsElement.EnumerateArray())
        {
            items.Add(ParseSchemaClosedPlanItem(
                itemElement,
                projectId,
                defaultMaxResults,
                defaultProviders,
                root.TryGetProperty("include_raw_data", out var rawRoot) && rawRoot.GetBoolean(),
                sourceIndex++));
        }

        return new ParsedSearchPlan(
            schemaId,
            schemaVersion,
            projectId,
            defaultMaxResults,
            root.TryGetProperty("include_raw_data", out var rawRootForResult) && rawRootForResult.GetBoolean(),
            language,
            defaultProviders,
            new ReadOnlyCollection<SearchPlanItem>(items.ToArray()),
            SearchPlanSource.SchemaClosed);
    }

    public static ParsedSearchPlan ParseLegacyImport(string jsonText)
    {
        using var document = JsonDocument.Parse(jsonText);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new SearchRuleException(SearchErrorCodes.InvalidPlanItemShape, "Search plan payload must be a JSON object.");
        }

        var schemaId = root.TryGetProperty("schema_id", out var schemaIdElement)
            ? Guard.NotBlank(schemaIdElement.GetString(), "schema_id")
            : SearchErrorCodes.KnownPlanSchemaId;
        var schemaVersion = root.TryGetProperty("schema_version", out var schemaVersionElement)
            ? Guard.NotBlank(schemaVersionElement.GetString(), "schema_version")
            : SearchErrorCodes.KnownPlanSchemaVersion;

        var projectId = root.TryGetProperty("project_id", out var projectIdElement)
            ? Guard.NotBlank(projectIdElement.GetString(), "project_id")
            : root.TryGetProperty("project", out var projectElement)
                ? Guard.NotBlank(projectElement.GetString(), "project")
                : "default-project";

        var maxResults = root.TryGetProperty("max_results", out var maxElement)
            ? maxElement.GetInt32()
            : 50;
        if (maxResults <= 0)
        {
            throw new SearchRuleException(SearchErrorCodes.NonPositiveMaxResults, "max_results must be positive.");
        }

        var includeRawData = root.TryGetProperty("include_raw_data", out var includeRawElement)
            ? includeRawElement.GetBoolean()
            : false;

        var language = root.TryGetProperty("language", out var languageElement)
            ? languageElement.GetString()
            : null;

        var defaultProviders = root.TryGetProperty("providers", out var providersElement)
            ? ParseAliasList(providersElement, SearchErrorCodes.InvalidPlanItemShape)
            : Array.Empty<string>();

        var searchProperty = root.TryGetProperty("searches", out var searchesElement)
            ? searchesElement
            : root.TryGetProperty("queries", out var queryElement)
                ? queryElement
                : throw new SearchRuleException(SearchErrorCodes.MissingPlanSearches, "Search plan requires searches or queries.");

        if (searchProperty.ValueKind != JsonValueKind.Array)
        {
            throw new SearchRuleException(SearchErrorCodes.InvalidPlanItemShape, "searches/queries must be an array.");
        }

        var items = new List<SearchPlanItem>();
        var sourceIndex = 0;
        foreach (var item in searchProperty.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new SearchRuleException(SearchErrorCodes.InvalidPlanItemShape, "Each plan item must be an object.");
            }

            var query = ExtractPlanString(item, "query", item.TryGetProperty("text", out var text) ? text.GetString() : null);
            var itemId = item.TryGetProperty("id", out var idElement)
                ? Guard.NotBlank(idElement.GetString(), "id")
                : $"legacy-{sourceIndex + 1}";
            var label = item.TryGetProperty("label", out var labelElement)
                ? labelElement.GetString()
                : null;

            var itemProjectId = item.TryGetProperty("project_id", out var itemProject)
                ? Guard.NotBlank(itemProject.GetString(), "project_id")
                : item.TryGetProperty("project", out var itemProjectLegacy)
                    ? Guard.NotBlank(itemProjectLegacy.GetString(), "project")
                    : projectId;

            var limit = item.TryGetProperty("max_results", out var max)
                ? max.GetInt32()
                : item.TryGetProperty("limit", out var legacyLimit)
                    ? legacyLimit.GetInt32()
                    : maxResults;

            int? yearFrom = item.TryGetProperty("year_from", out var yearFromElement)
                ? yearFromElement.GetInt32()
                : item.TryGetProperty("year_min", out var yearMin)
                    ? yearMin.GetInt32()
                    : null;

            int? yearTo = item.TryGetProperty("year_to", out var yearToElement)
                ? yearToElement.GetInt32()
                : item.TryGetProperty("year_max", out var yearMax)
                    ? yearMax.GetInt32()
                    : null;

            var itemProviders = item.TryGetProperty("providers", out var itemProvidersElement)
                ? ParseAliasList(itemProvidersElement, SearchErrorCodes.UnknownPlanItemField)
                : defaultProviders;

            var itemIncludeRaw = item.TryGetProperty("include_raw_data", out var itemRaw)
                ? itemRaw.GetBoolean()
                : includeRawData;

            items.Add(new SearchPlanItem(
                itemId,
                label,
                query,
                itemProjectId,
                limit,
                yearFrom,
                yearTo,
                itemProviders,
                itemIncludeRaw,
                sourceIndex++));
        }

        return new ParsedSearchPlan(
            schemaId,
            schemaVersion,
            projectId,
            maxResults,
            includeRawData,
            language,
            defaultProviders,
            new ReadOnlyCollection<SearchPlanItem>(items.ToArray()),
            SearchPlanSource.PhpLegacyImport);
    }

    private static SearchPlanItem ParseSchemaClosedPlanItem(
        JsonElement item,
        string defaultProjectId,
        int defaultMaxResults,
        IReadOnlyList<string> defaultProviders,
        bool defaultIncludeRawData,
        int sourceIndex)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            throw new SearchRuleException(SearchErrorCodes.InvalidPlanItemShape, "Each search plan item must be an object.");
        }

        EnsureKnownItemFields(item);

        var itemId = ExtractPlanString(item, "id", null);
        var query = ExtractPlanString(item, "query", null);
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new SearchRuleException(SearchErrorCodes.InvalidPlanItemShape, "Search plan item requires a query.");
        }

        var label = item.TryGetProperty("label", out var labelElement)
            ? labelElement.GetString()
            : null;

        var itemProjectId = item.TryGetProperty("project_id", out var itemProject)
            ? Guard.NotBlank(itemProject.GetString(), nameof(itemProject))
            : defaultProjectId;

        var limit = item.TryGetProperty("limit", out var limitElement)
            ? limitElement.GetInt32()
            : item.TryGetProperty("max_results", out var maxElement)
                ? maxElement.GetInt32()
                : defaultMaxResults;

        if (limit <= 0)
        {
            throw new SearchRuleException(SearchErrorCodes.NonPositiveMaxResults, "Search item max_results must be positive.");
        }

        int? yearFrom = item.TryGetProperty("year_from", out var yearFromElement)
            ? yearFromElement.GetInt32()
            : null;

        int? yearTo = item.TryGetProperty("year_to", out var yearToElement)
            ? yearToElement.GetInt32()
            : null;

        var providers = item.TryGetProperty("providers", out var providersElement)
            ? ParseAliasList(providersElement, SearchErrorCodes.UnknownPlanItemField)
            : defaultProviders;

        var includeRawData = item.TryGetProperty("include_raw_data", out var rawElement)
            ? rawElement.GetBoolean()
            : defaultIncludeRawData;

        return new SearchPlanItem(
            itemId,
            label,
            query,
            itemProjectId,
            limit,
            yearFrom,
            yearTo,
            providers,
            includeRawData,
            sourceIndex);
    }

    private static void EnsureKnownRootFields(JsonElement root)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (!SearchErrorCodes.KnownPlanRootFields.Contains(property.Name))
            {
                throw new SearchRuleException(SearchErrorCodes.UnknownPlanRootField, $"Unknown root plan field '{property.Name}'.");
            }
        }
    }

    private static void EnsureKnownItemFields(JsonElement item)
    {
        foreach (var property in item.EnumerateObject())
        {
            if (!SearchErrorCodes.KnownPlanItemFields.Contains(property.Name))
            {
                throw new SearchRuleException(SearchErrorCodes.UnknownPlanItemField, $"Unknown plan item field '{property.Name}'.");
            }
        }
    }

    private static IReadOnlyList<string> ParseAliasList(JsonElement token, string unknownFieldCategory)
    {
        if (token.ValueKind == JsonValueKind.Array)
        {
            var aliases = token
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();

            return SearchService.NormalizeProviderAliases(aliases);
        }

        if (token.ValueKind == JsonValueKind.String)
        {
            var raw = token.GetString() ?? string.Empty;
            var aliases = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return SearchService.NormalizeProviderAliases(aliases);
        }

        throw new SearchRuleException(unknownFieldCategory, "provider aliases must be a list of strings or comma-separated aliases.");
    }

    private static string ExtractPlanString(JsonElement item, string requiredName, string? fallback)
    {
        if (!item.TryGetProperty(requiredName, out var value))
        {
            if (fallback is null)
            {
                throw new SearchRuleException(SearchErrorCodes.InvalidPlanItemShape, $"Missing required plan field '{requiredName}'.");
            }

            return Guard.NotBlank(fallback, $"plan.{requiredName}");
        }

        return Guard.NotBlank(value.GetString(), requiredName);
    }
}
