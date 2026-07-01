using System.Globalization;
using System.Text;
using System.Text.Json;
using NexusScholar.UiContracts;

namespace NexusScholar.Cli.ResearchWorkspace;

internal static class WorkspacePlanTextRenderer
{
    public static string RenderReview(WorkspacePlan plan)
    {
        var warnings = WarningItems(plan).ToArray();
        var candidates = ReviewCandidates(plan).ToArray();
        var gates = plan.Blocks.Where(block => string.Equals(block.Kind, KnownBlockKinds.HumanGateMergeDecision, StringComparison.Ordinal)).ToArray();
        var builder = new StringBuilder();

        builder.AppendLine("Review queue");
        builder.AppendLine($"Workspace: {plan.Title}");
        builder.AppendLine($"Mode: {plan.Mode}");
        builder.AppendLine();
        builder.AppendLine("Blocking");
        builder.AppendLine($"  {Plural(gates.Length, "human merge decision", "human merge decisions")} {Verb(gates.Length, "requires", "require")} review");
        builder.AppendLine();
        builder.AppendLine("Review required");
        builder.AppendLine($"  {Plural(candidates.Length, "duplicate record comparison", "duplicate record comparisons")}");
        builder.AppendLine($"  {Plural(warnings.Length, "import warning category", "import warning categories")}");

        if (candidates.Length > 0 || warnings.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Top items");
            foreach (var candidate in candidates)
            {
                builder.AppendLine($"  [{candidate.DisplayId}] {candidate.Title}");
                builder.AppendLine($"      Reason: {candidate.Reason}");
                builder.AppendLine($"      Command: nexus clusters show {candidate.DisplayId}");
            }

            foreach (var warning in warnings)
            {
                builder.AppendLine($"  [import-warning] {warning.Category}: {Plural(warning.Count, "warning", "warnings")}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("APP-01 note");
        builder.AppendLine("  Merge actions are placeholders only. This CLI does not accept, reject, or execute merge decisions yet.");
        return builder.ToString();
    }

    public static string RenderClustersSummary(WorkspacePlan plan)
    {
        var clusters = ClusterItems(plan).ToArray();
        var candidates = ReviewCandidates(plan).ToArray();
        var builder = new StringBuilder();

        builder.AppendLine("Duplicate clusters");
        builder.AppendLine();
        builder.AppendLine("Exact duplicate clusters");
        builder.AppendLine($"  {Plural(clusters.Length, "cluster", "clusters")}");
        builder.AppendLine();
        builder.AppendLine("Review-required candidates");
        builder.AppendLine($"  {Plural(candidates.Length, "candidate", "candidates")}");
        builder.AppendLine();
        builder.AppendLine("Use:");
        builder.AppendLine("  nexus clusters exact");
        builder.AppendLine("  nexus clusters review");
        builder.AppendLine("  nexus clusters show <id>");
        return builder.ToString();
    }

    public static string RenderClustersExact(WorkspacePlan plan)
    {
        var clusters = ClusterItems(plan).ToArray();
        var builder = new StringBuilder();

        builder.AppendLine("Exact duplicate clusters");
        builder.AppendLine();
        foreach (var cluster in clusters)
        {
            builder.AppendLine($"[{cluster.DisplayId}]");
            builder.AppendLine($"  Members: {cluster.MemberCount}");
            builder.AppendLine($"  Representative: {cluster.RepresentativeTitle}");
            builder.AppendLine($"  Match basis: {JoinOrNone(cluster.MatchBasisItems)}");
            builder.AppendLine($"  Show: nexus clusters show {cluster.DisplayId}");
        }

        if (clusters.Length == 0)
        {
            builder.AppendLine("No exact duplicate clusters found.");
        }

        builder.AppendLine();
        builder.AppendLine("APP-01 note");
        builder.AppendLine("  This command displays generated analysis output only. It does not execute merge decisions.");
        return builder.ToString();
    }

    public static string RenderClustersReview(WorkspacePlan plan)
    {
        var candidates = ReviewCandidates(plan).ToArray();
        var builder = new StringBuilder();

        builder.AppendLine("Review-required duplicate candidates");
        builder.AppendLine();
        foreach (var candidate in candidates)
        {
            builder.AppendLine($"[{candidate.DisplayId}]");
            builder.AppendLine($"  Score: {FormatNumber(candidate.Score)}");
            builder.AppendLine($"  Reason: {candidate.Reason}");
            builder.AppendLine($"  Left:  {candidate.Left.Title}");
            builder.AppendLine($"  Right: {candidate.Right.Title}");
            builder.AppendLine($"  Sources: {JoinOrNone(new[] { candidate.Left.Source, candidate.Right.Source }.WhereNotBlank())}");
            builder.AppendLine($"  Show: nexus clusters show {candidate.DisplayId}");
        }

        if (candidates.Length == 0)
        {
            builder.AppendLine("No review-required duplicate candidates found.");
        }

        return builder.ToString();
    }

    public static string? RenderClusterShow(WorkspacePlan plan, string id)
    {
        var normalizedId = id.Trim();
        var candidates = ReviewCandidates(plan).ToArray();
        var candidate = candidates.FirstOrDefault(item => item.Matches(normalizedId));
        if (candidate is not null)
        {
            return RenderCandidate(candidate);
        }

        var cluster = ClusterItems(plan).FirstOrDefault(item => item.Matches(normalizedId));
        return cluster is null ? null : RenderCluster(cluster);
    }

    private static string RenderCandidate(ReviewCandidateItem candidate)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Candidate: {candidate.DisplayId}");
        builder.AppendLine("Status: review required");
        builder.AppendLine();
        builder.AppendLine("Left record");
        builder.AppendLine($"  Id: {candidate.Left.Id}");
        builder.AppendLine($"  Title: {candidate.Left.Title}");
        AppendOptional(builder, "  Year: ", candidate.Left.Year);
        AppendOptional(builder, "  Source: ", candidate.Left.Source);
        AppendOptional(builder, "  Source record: ", candidate.Left.SourceRecordId);
        AppendOptional(builder, "  Source trace: ", candidate.Left.SourceTraceId);
        AppendOptional(builder, "  Work IDs: ", JoinOrNull(candidate.Left.WorkIds));
        AppendOptional(builder, "  Source-specific IDs: ", JoinOrNull(candidate.Left.SourceSpecificIds));
        builder.AppendLine();
        builder.AppendLine("Right record");
        builder.AppendLine($"  Id: {candidate.Right.Id}");
        builder.AppendLine($"  Title: {candidate.Right.Title}");
        AppendOptional(builder, "  Year: ", candidate.Right.Year);
        AppendOptional(builder, "  Source: ", candidate.Right.Source);
        AppendOptional(builder, "  Source record: ", candidate.Right.SourceRecordId);
        AppendOptional(builder, "  Source trace: ", candidate.Right.SourceTraceId);
        AppendOptional(builder, "  Work IDs: ", JoinOrNull(candidate.Right.WorkIds));
        AppendOptional(builder, "  Source-specific IDs: ", JoinOrNull(candidate.Right.SourceSpecificIds));
        builder.AppendLine();
        builder.AppendLine("Why Nexus flagged this");
        builder.AppendLine($"  {candidate.Reason}");
        AppendOptional(builder, "  Score: ", FormatNumber(candidate.Score));
        AppendOptional(builder, "  Threshold: ", FormatNumber(candidate.Threshold));
        builder.AppendLine();
        builder.AppendLine("APP-01 action");
        builder.AppendLine("  Human merge decision required.");
        builder.AppendLine("  This CLI version only displays the decision gate.");
        return builder.ToString();
    }

    private static string RenderCluster(ClusterItem cluster)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Cluster: {cluster.DisplayId}");
        builder.AppendLine("Status: exact duplicate cluster");
        builder.AppendLine();
        builder.AppendLine($"Members: {cluster.MemberCount}");
        builder.AppendLine($"Representative: {cluster.RepresentativeTitle}");
        AppendOptional(builder, "Representative candidate: ", cluster.RepresentativeCandidateId);
        AppendOptional(builder, "Representative work IDs: ", JoinOrNull(cluster.WorkIds));
        builder.AppendLine();
        builder.AppendLine("Member IDs");
        foreach (var memberId in cluster.MemberIds)
        {
            builder.AppendLine($"  - {memberId}");
        }

        builder.AppendLine();
        builder.AppendLine("Evidence");
        builder.AppendLine($"  Match basis: {JoinOrNone(cluster.MatchBasisItems)}");
        builder.AppendLine();
        builder.AppendLine("APP-01 action");
        builder.AppendLine("  This CLI version only displays the duplicate cluster.");
        return builder.ToString();
    }

    private static IEnumerable<WarningItem> WarningItems(WorkspacePlan plan)
    {
        foreach (var block in plan.Blocks.Where(block => string.Equals(block.Kind, KnownBlockKinds.ImportWarningSummary, StringComparison.Ordinal)))
        {
            using var payload = ParsePayload(block);
            var root = payload?.RootElement;
            var category = root.HasValue ? StringProperty(root.Value, "category") : null;
            var count = root.HasValue ? IntProperty(root.Value, "warning_count", "count") : null;
            yield return new WarningItem(
                string.IsNullOrWhiteSpace(category) ? block.Title : category,
                count ?? 1);
        }
    }

    private static IEnumerable<ClusterItem> ClusterItems(WorkspacePlan plan)
    {
        var index = 0;
        foreach (var block in plan.Blocks.Where(block => string.Equals(block.Kind, KnownBlockKinds.DedupCandidateCluster, StringComparison.Ordinal)))
        {
            index++;
            using var payload = ParsePayload(block);
            var root = payload?.RootElement;
            var clusterId = root.HasValue ? StringProperty(root.Value, "cluster_id", "clusterId") : null;
            var memberIds = root.HasValue ? StringArrayProperty(root.Value, "member_ids", "memberIds") : Array.Empty<string>();
            var evidenceKinds = root.HasValue ? StringArrayProperty(root.Value, "evidence_kinds", "evidenceKinds") : Array.Empty<string>();
            var matchBasis = root.HasValue ? StringProperty(root.Value, "match_basis", "matchBasis") : null;
            var doi = root.HasValue ? StringProperty(root.Value, "doi") : null;
            var workIds = root.HasValue ? StringArrayProperty(root.Value, "representative_work_ids", "representativeWorkIds") : Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(doi))
            {
                workIds = workIds.Concat(new[] { $"doi:{doi}" }).Distinct(StringComparer.Ordinal).ToArray();
            }

            yield return new ClusterItem(
                string.IsNullOrWhiteSpace(clusterId) ? string.Create(CultureInfo.InvariantCulture, $"dedup-cluster-{index:0000}") : clusterId,
                block.BlockId,
                block.Title,
                IntProperty(root, "member_count", "memberCount") ?? memberIds.Count,
                StringProperty(root, "representative_title", "representativeTitle") ?? block.Title,
                StringProperty(root, "representative_candidate_id", "representativeCandidateId"),
                memberIds,
                workIds,
                evidenceKinds.Concat(new[] { matchBasis }).WhereNotBlank().ToArray());
        }
    }

    private static IEnumerable<ReviewCandidateItem> ReviewCandidates(WorkspacePlan plan)
    {
        var index = 0;
        foreach (var block in plan.Blocks.Where(block => string.Equals(block.Kind, KnownBlockKinds.DedupRecordComparison, StringComparison.Ordinal)))
        {
            index++;
            using var payload = ParsePayload(block);
            var root = payload?.RootElement;
            var displayId = StringProperty(root, "candidateId") ?? string.Create(CultureInfo.InvariantCulture, $"dedup-candidate-{index:0000}");
            var candidateAId = StringProperty(root, "candidate_a_id", "candidateAId") ?? string.Empty;
            var candidateBId = StringProperty(root, "candidate_b_id", "candidateBId") ?? string.Empty;
            var left = CandidateDisplay(root, "candidate_a", "left", candidateAId);
            var right = CandidateDisplay(root, "candidate_b", "right", candidateBId);
            var reason = StringProperty(root, "review_reason", "reason") ??
                block.ValidationRefs.Select(item => item.Message).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ??
                block.Summary ??
                "review required";

            yield return new ReviewCandidateItem(
                displayId,
                block.BlockId,
                block.Title,
                left,
                right,
                DoubleProperty(root, "title_similarity", "score"),
                DoubleProperty(root, "threshold_used", "threshold"),
                reason);
        }
    }

    private static CandidateRecordDisplay CandidateDisplay(JsonElement? root, string snakeName, string camelName, string fallbackId)
    {
        if (root.HasValue && TryGetProperty(root.Value, out var candidateRoot, snakeName, camelName) && candidateRoot.ValueKind == JsonValueKind.Object)
        {
            var recordId = StringProperty(candidateRoot, "candidate_id", "recordId") ?? fallbackId;
            return new CandidateRecordDisplay(
                recordId,
                StringProperty(candidateRoot, "title") ?? recordId,
                StringProperty(candidateRoot, "year"),
                StringProperty(candidateRoot, "source", "source_trace_id", "sourceTraceId"),
                StringProperty(candidateRoot, "source_record_id", "sourceRecordId"),
                StringProperty(candidateRoot, "source_trace_id", "sourceTraceId"),
                StringArrayProperty(candidateRoot, "work_ids", "workIds"),
                StringArrayProperty(candidateRoot, "source_specific_ids", "sourceSpecificIds"));
        }

        return new CandidateRecordDisplay(
            fallbackId,
            string.IsNullOrWhiteSpace(fallbackId) ? "unknown" : fallbackId,
            null,
            null,
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<string>());
    }

    private static JsonDocument? ParsePayload(ResearchBlockDescriptor block)
    {
        return string.IsNullOrWhiteSpace(block.PayloadJson) ? null : JsonDocument.Parse(block.PayloadJson);
    }

    private static string? StringProperty(JsonElement? root, params string[] names) =>
        root.HasValue ? StringProperty(root.Value, names) : null;

    private static string? StringProperty(JsonElement root, params string[] names)
    {
        if (!TryGetProperty(root, out var property, names))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static int? IntProperty(JsonElement? root, params string[] names)
    {
        if (!root.HasValue || !TryGetProperty(root.Value, out var property, names))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value) ? value : null;
    }

    private static double? DoubleProperty(JsonElement? root, params string[] names)
    {
        if (!root.HasValue || !TryGetProperty(root.Value, out var property, names))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value) ? value : null;
    }

    private static IReadOnlyList<string> StringArrayProperty(JsonElement root, params string[] names)
    {
        if (!TryGetProperty(root, out var property, names) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return property.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
            .WhereNotBlank()
            .ToArray();
    }

    private static bool TryGetProperty(JsonElement root, out JsonElement property, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out property))
            {
                return true;
            }
        }

        property = default;
        return false;
    }

    private static void AppendOptional(StringBuilder builder, string prefix, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"{prefix}{value}");
        }
    }

    private static string? JoinOrNull(IEnumerable<string> values)
    {
        var items = values.WhereNotBlank().ToArray();
        return items.Length == 0 ? null : string.Join(", ", items);
    }

    private static string JoinOrNone(IEnumerable<string> values) =>
        JoinOrNull(values) ?? "none";

    private static string FormatNumber(double? value) =>
        value.HasValue ? value.Value.ToString("0.####", CultureInfo.InvariantCulture) : "n/a";

    private static string Plural(int count, string singular, string plural) =>
        string.Create(CultureInfo.InvariantCulture, $"{count} {(count == 1 ? singular : plural)}");

    private static string Verb(int count, string singular, string plural) =>
        count == 1 ? singular : plural;

    private sealed record WarningItem(string Category, int Count);

    private sealed record ClusterItem(
        string DisplayId,
        string BlockId,
        string Title,
        int MemberCount,
        string RepresentativeTitle,
        string? RepresentativeCandidateId,
        IReadOnlyList<string> MemberIds,
        IReadOnlyList<string> WorkIds,
        IReadOnlyList<string> MatchBasisItems)
    {
        public bool Matches(string id) =>
            string.Equals(DisplayId, id, StringComparison.Ordinal) ||
            string.Equals(BlockId, id, StringComparison.Ordinal);
    }

    private sealed record ReviewCandidateItem(
        string DisplayId,
        string BlockId,
        string Title,
        CandidateRecordDisplay Left,
        CandidateRecordDisplay Right,
        double? Score,
        double? Threshold,
        string Reason)
    {
        public bool Matches(string id) =>
            string.Equals(DisplayId, id, StringComparison.Ordinal) ||
            string.Equals(BlockId, id, StringComparison.Ordinal) ||
            string.Equals(Left.Id, id, StringComparison.Ordinal) ||
            string.Equals(Right.Id, id, StringComparison.Ordinal) ||
            string.Equals($"{Left.Id}|{Right.Id}", id, StringComparison.Ordinal);
    }

    private sealed record CandidateRecordDisplay(
        string Id,
        string Title,
        string? Year,
        string? Source,
        string? SourceRecordId,
        string? SourceTraceId,
        IReadOnlyList<string> WorkIds,
        IReadOnlyList<string> SourceSpecificIds);
}

internal static class WorkspacePlanTextRendererEnumerableExtensions
{
    public static IEnumerable<string> WhereNotBlank(this IEnumerable<string?> values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim());
}
