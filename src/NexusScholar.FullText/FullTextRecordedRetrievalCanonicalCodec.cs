using System.Globalization;
using System.Text.Json;
using NexusScholar.Kernel;

namespace NexusScholar.FullText;

public static class FullTextRecordedRetrievalCanonicalCodec
{
    private static readonly string[] RequiredFields =
    [
        "access_route",
        "artifact_kind",
        "byte_length",
        "evidence_id",
        "http_status",
        "input_digest",
        "media_type",
        "raw_byte_digest",
        "raw_byte_digest_scope",
        "received_at",
        "redirect_chain",
        "response_complete",
        "retention_disposition",
        "rights_reference",
        "rights_status",
        "source_alias",
        "source_reference",
        "requested_at"
    ];

    private static readonly string[] OptionalFields =
    [
        "content_encoding",
        "terminal_failure_category",
        "terminal_failure_summary"
    ];

    public static byte[] Serialize(FullTextRecordedRetrievalEvidence evidence) =>
        (evidence ?? throw new ArgumentNullException(nameof(evidence))).ToCanonicalBytes();

    public static FullTextRecordedRetrievalEvidence Rehydrate(
        byte[] bytes,
        ContentDigest expectedDigest,
        FullTextInput input,
        byte[] exactBytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(exactBytes);

        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (CanonicalJsonValue.FromJsonElement(document.RootElement) is not CanonicalJsonObject root ||
                !bytes.SequenceEqual(CanonicalJsonSerializer.SerializeToUtf8Bytes(root)))
            {
                throw Rule("Recorded retrieval evidence bytes must be exact canonical JSON.");
            }

            var verified = DigestEnvelope.RehydrateAndVerify(
                document.RootElement,
                expectedDigest,
                DigestScope.CanonicalJsonRecord,
                FullTextRecordedRetrievalEvidence.SchemaId,
                FullTextRecordedRetrievalEvidence.SchemaVersion);
            var content = verified.Envelope.Content;
            RequireExact(content);

            var inputDigest = ContentDigest.Sha256(FullTextAuthorityCanonicalCodec.Serialize(input));
            if (ContentDigest.Parse(Text(content, "input_digest")) != inputDigest)
            {
                throw Rule("Recorded retrieval evidence does not bind the supplied Full Text input.");
            }

            if (!string.Equals(
                    Text(content, "raw_byte_digest_scope"),
                    DigestScope.RawArtifactBytes.ToString(),
                    StringComparison.Ordinal))
            {
                throw Rule("Recorded retrieval evidence has an invalid raw-byte digest scope.");
            }

            var redirects = Array(content, "redirect_chain")
                .Select(value =>
                {
                    var item = value as CanonicalJsonObject ??
                        throw Rule("Recorded redirect evidence must be an object.");
                    RequireExact(item, ["status_code", "url"], []);
                    return new FullTextRecordedRedirect(Text(item, "url"), Integer(item, "status_code"));
                })
                .ToArray();

            var restored = FullTextRecordedRetrievalEvidence.Record(
                Text(content, "evidence_id"),
                input,
                Text(content, "source_alias"),
                Text(content, "source_reference"),
                Text(content, "access_route"),
                Text(content, "rights_status"),
                Text(content, "rights_reference"),
                Text(content, "artifact_kind"),
                Text(content, "media_type"),
                Integer(content, "http_status"),
                exactBytes,
                Timestamp(content, "requested_at"),
                Timestamp(content, "received_at"),
                Boolean(content, "response_complete"),
                OptionalText(content, "content_encoding"),
                redirects,
                OptionalText(content, "terminal_failure_category"),
                OptionalText(content, "terminal_failure_summary"),
                Text(content, "retention_disposition"));

            if (Long(content, "byte_length") != exactBytes.LongLength ||
                !string.Equals(Text(content, "raw_byte_digest"), restored.RawByteDigest, StringComparison.Ordinal) ||
                restored.Digest != expectedDigest ||
                !bytes.SequenceEqual(restored.ToCanonicalBytes()))
            {
                throw Rule("Recorded retrieval evidence did not reproduce from exact bytes.");
            }

            return restored;
        }
        catch (FullTextRuleException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or FormatException or InvalidOperationException or OverflowException)
        {
            throw Rule($"Recorded retrieval evidence rehydration failed: {exception.Message}");
        }
    }

    private static void RequireExact(CanonicalJsonObject content) =>
        RequireExact(content, RequiredFields, OptionalFields);

    private static void RequireExact(
        CanonicalJsonObject content,
        IEnumerable<string> required,
        IEnumerable<string> optional)
    {
        var requiredSet = required.ToHashSet(StringComparer.Ordinal);
        var allowed = requiredSet.Concat(optional).ToHashSet(StringComparer.Ordinal);
        if (!requiredSet.IsSubsetOf(content.Properties.Keys) ||
            content.Properties.Keys.Any(key => !allowed.Contains(key)) ||
            content.Properties.ContainsKey("terminal_failure_category") !=
            content.Properties.ContainsKey("terminal_failure_summary"))
        {
            throw Rule("Recorded retrieval evidence has missing, unknown, or inconsistent fields.");
        }
    }

    private static IReadOnlyList<CanonicalJsonValue> Array(CanonicalJsonObject root, string name) =>
        Value(root, name) is CanonicalJsonArray array
            ? array.Items
            : throw Rule($"Recorded retrieval field '{name}' must be an array.");

    private static bool Boolean(CanonicalJsonObject root, string name) =>
        Value(root, name) is CanonicalJsonBoolean boolean
            ? boolean.Value
            : throw Rule($"Recorded retrieval field '{name}' must be boolean.");

    private static int Integer(CanonicalJsonObject root, string name) =>
        Value(root, name) is CanonicalJsonNumber number &&
        int.TryParse(number.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw Rule($"Recorded retrieval field '{name}' must be an integer.");

    private static long Long(CanonicalJsonObject root, string name) =>
        Value(root, name) is CanonicalJsonNumber number &&
        long.TryParse(number.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw Rule($"Recorded retrieval field '{name}' must be an integer.");

    private static string? OptionalText(CanonicalJsonObject root, string name) =>
        root.Properties.ContainsKey(name) ? Text(root, name) : null;

    private static string Text(CanonicalJsonObject root, string name) =>
        Value(root, name) is CanonicalJsonString text
            ? text.Value
            : throw Rule($"Recorded retrieval field '{name}' must be text.");

    private static DateTimeOffset Timestamp(CanonicalJsonObject root, string name)
    {
        var value = Text(root, name);
        CanonicalTimestamp.ValidateCanonicalUtc(value);
        return DateTimeOffset.ParseExact(
            value,
            CanonicalTimestamp.DefaultUtcFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private static CanonicalJsonValue Value(CanonicalJsonObject root, string name) =>
        root.Properties.TryGetValue(name, out var value)
            ? value
            : throw Rule($"Recorded retrieval field '{name}' is required.");

    private static FullTextRuleException Rule(string message) =>
        new(FullTextRetrievalErrorCodes.InvalidEvidence, message);
}
