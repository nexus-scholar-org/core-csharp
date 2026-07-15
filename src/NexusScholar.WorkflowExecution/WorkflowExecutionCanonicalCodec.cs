using System.Globalization;
using System.Text.Json;
using NexusScholar.Kernel;
using NexusScholar.Workflow;

namespace NexusScholar.WorkflowExecution;

public static class WorkflowExecutionCanonicalCodec
{
    public static byte[] Serialize(WorkflowExecutionAuthorityPolicy policy) =>
        CanonicalJsonSerializer.SerializeToUtf8Bytes((policy ?? throw new ArgumentNullException(nameof(policy))).ToCanonicalJson());

    public static byte[] Serialize(WorkflowExecutionHeader header) =>
        CanonicalJsonSerializer.SerializeToUtf8Bytes((header ?? throw new ArgumentNullException(nameof(header))).ToCanonicalJson());

    public static byte[] Serialize(WorkflowExecutionEvent item) =>
        CanonicalJsonSerializer.SerializeToUtf8Bytes((item ?? throw new ArgumentNullException(nameof(item))).ToCanonicalJson());

    public static WorkflowExecutionAuthorityPolicy RehydratePolicy(
        byte[] bytes,
        ContentDigest expectedDigest,
        VerifiedWorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var content = ParseEnvelope(bytes, expectedDigest, "nexus.workflow-execution.authority-policy");
        RequireExact(content, new[]
        {
            "approved_at", "approved_by", "assignments", "execution_scope", "policy_id",
            "protocol_content_digest", "protocol_version_id", "workflow_digest", "workflow_id"
        });
        var policy = WorkflowExecutionAuthorityPolicy.Create(
            String(content, "policy_id"),
            Ref(Object(content, "execution_scope")),
            workflow,
            Array(content, "assignments").Select(Assignment),
            Actor(Object(content, "approved_by")),
            Timestamp(content, "approved_at"));
        RequireDigest(policy.Digest, expectedDigest, "Execution authority policy");
        return policy;
    }

    public static WorkflowExecutionHeader RehydrateHeader(
        byte[] bytes,
        ContentDigest expectedDigest,
        VerifiedWorkflowDefinition workflow,
        WorkflowExecutionAuthorityPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(policy);
        var content = ParseEnvelope(bytes, expectedDigest, "nexus.workflow-execution.header");
        RequireExact(content, new[]
        {
            "authority_policy_digest", "authority_policy_id", "created_at", "created_by", "execution_id",
            "execution_scope", "node_ids", "protocol_content_digest", "protocol_id", "protocol_version_id",
            "protocol_version_number", "workflow_digest", "workflow_id"
        });
        var header = WorkflowExecutionHeader.Create(
            String(content, "execution_id"), workflow, policy, Actor(Object(content, "created_by")), Timestamp(content, "created_at"));
        RequireDigest(header.Digest, expectedDigest, "Execution header");
        return header;
    }

    public static WorkflowExecutionEvent RehydrateEvent(
        byte[] bytes,
        ContentDigest expectedDigest,
        WorkflowExecutionHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        var content = ParseEnvelope(bytes, expectedDigest, "nexus.workflow-execution.event");
        RequireExact(content, new[]
        {
            "actor", "approvals", "authority_policy_digest", "authority_policy_id", "event_id", "event_kind",
            "execution_id", "expected_prior_state", "inputs", "node_id", "occurred_at", "ordinal", "outputs",
            "previous_digest", "protocol_content_digest", "protocol_version_id", "rationale", "request_digest",
            "request_id", "resulting_state", "workflow_digest", "workflow_id"
        }, new[]
        {
            "attempt_id", "attempt_sequence", "decision", "error_category", "error_summary",
            "invalidation_policy_ref", "invalidation_source", "successor_execution"
        });
        var item = WorkflowExecutionEvent.Create(
            header,
            Integer(content, "ordinal"),
            Digest(content, "previous_digest"),
            String(content, "request_id"),
            String(content, "node_id"),
            EventKind(String(content, "event_kind")),
            State(String(content, "expected_prior_state")),
            State(String(content, "resulting_state")),
            Actor(Object(content, "actor")),
            Timestamp(content, "occurred_at"),
            String(content, "rationale"),
            OptionalString(content, "attempt_id"),
            OptionalInteger(content, "attempt_sequence"),
            Array(content, "inputs").Select(value => Ref(AsObject(value))),
            Array(content, "outputs").Select(value => Ref(AsObject(value))),
            Array(content, "approvals").Select(Approval),
            OptionalObject(content, "decision", Ref),
            OptionalString(content, "error_category"),
            OptionalString(content, "error_summary"),
            OptionalObject(content, "invalidation_source", Ref),
            OptionalString(content, "invalidation_policy_ref"),
            OptionalObject(content, "successor_execution", Ref));
        RequireDigest(item.Digest, expectedDigest, "Execution event");
        return item;
    }

    private static CanonicalJsonObject ParseEnvelope(byte[] bytes, ContentDigest expectedDigest, string schemaId)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var parsed = CanonicalJsonValue.FromJsonElement(document.RootElement);
        if (parsed is not CanonicalJsonObject root || !bytes.SequenceEqual(CanonicalJsonSerializer.SerializeToUtf8Bytes(root)))
            throw Rule("Canonical execution record bytes are required.");
        try
        {
            return DigestEnvelope.RehydrateAndVerify(
                document.RootElement, expectedDigest, DigestScope.CanonicalJsonRecord, schemaId, "1.0.0").Envelope.Content;
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or FormatException)
        {
            throw Rule("Execution record envelope verification failed.", exception);
        }
    }

    private static WorkflowExecutionRecordRef Ref(CanonicalJsonObject value)
    {
        RequireExact(value, new[] { "digest", "id", "kind" });
        return new WorkflowExecutionRecordRef(String(value, "kind"), String(value, "id"), Digest(value, "digest"));
    }

    private static WorkflowExecutionActor Actor(CanonicalJsonObject value)
    {
        RequireExact(value, new[] { "actor_id", "kind", "role" });
        return new WorkflowExecutionActor(String(value, "actor_id"), String(value, "kind"), String(value, "role"));
    }

    private static WorkflowExecutionRoleAssignment Assignment(CanonicalJsonValue value)
    {
        var obj = AsObject(value);
        RequireExact(obj, new[] { "actor_id", "role" });
        return new WorkflowExecutionRoleAssignment(String(obj, "actor_id"), String(obj, "role"));
    }

    private static WorkflowExecutionApproval Approval(CanonicalJsonValue value)
    {
        var obj = AsObject(value);
        RequireExact(obj, new[] { "actor", "record" });
        return new WorkflowExecutionApproval(Actor(Object(obj, "actor")), Ref(Object(obj, "record")));
    }

    private static WorkflowExecutionState State(string value) => value switch
    {
        "pending" => WorkflowExecutionState.Pending,
        "ready" => WorkflowExecutionState.Ready,
        "active" => WorkflowExecutionState.Active,
        "blocked" => WorkflowExecutionState.Blocked,
        "completed" => WorkflowExecutionState.Completed,
        "failed" => WorkflowExecutionState.Failed,
        "invalidated" => WorkflowExecutionState.Invalidated,
        "superseded" => WorkflowExecutionState.Superseded,
        _ => throw Rule("Unknown persisted execution state.")
    };

    private static WorkflowExecutionEventKind EventKind(string value) => value switch
    {
        "dependencies-satisfied" => WorkflowExecutionEventKind.DependenciesSatisfied,
        "work-started" => WorkflowExecutionEventKind.WorkStarted,
        "work-blocked" => WorkflowExecutionEventKind.WorkBlocked,
        "block-cleared" => WorkflowExecutionEventKind.BlockCleared,
        "work-completed" => WorkflowExecutionEventKind.WorkCompleted,
        "work-failed" => WorkflowExecutionEventKind.WorkFailed,
        "retry-authorized" => WorkflowExecutionEventKind.RetryAuthorized,
        "work-invalidated" => WorkflowExecutionEventKind.WorkInvalidated,
        "successor-bound" => WorkflowExecutionEventKind.SuccessorBound,
        _ => throw Rule("Unknown persisted execution event kind.")
    };

    private static void RequireExact(CanonicalJsonObject value, IEnumerable<string> required, IEnumerable<string>? optional = null)
    {
        var requiredSet = required.ToHashSet(StringComparer.Ordinal);
        var allowed = requiredSet.Concat(optional ?? System.Array.Empty<string>()).ToHashSet(StringComparer.Ordinal);
        if (!requiredSet.IsSubsetOf(value.Properties.Keys) || value.Properties.Keys.Any(key => !allowed.Contains(key)))
            throw Rule("Execution canonical record has missing or unknown fields.");
    }

    private static CanonicalJsonObject Object(CanonicalJsonObject root, string name) => AsObject(Value(root, name));

    private static CanonicalJsonObject AsObject(CanonicalJsonValue value) => value as CanonicalJsonObject
        ?? throw Rule("Execution canonical field must be an object.");

    private static IReadOnlyList<CanonicalJsonValue> Array(CanonicalJsonObject root, string name) => Value(root, name) switch
    {
        CanonicalJsonArray array => array.Items,
        _ => throw Rule($"Execution canonical field '{name}' must be an array.")
    };

    private static string String(CanonicalJsonObject root, string name) => Value(root, name) switch
    {
        CanonicalJsonString text => text.Value,
        _ => throw Rule($"Execution canonical field '{name}' must be a string.")
    };

    private static string? OptionalString(CanonicalJsonObject root, string name) => root.Properties.ContainsKey(name) ? String(root, name) : null;

    private static int Integer(CanonicalJsonObject root, string name) => Value(root, name) switch
    {
        CanonicalJsonNumber number when int.TryParse(number.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) => result,
        _ => throw Rule($"Execution canonical field '{name}' must be an integer.")
    };

    private static int? OptionalInteger(CanonicalJsonObject root, string name) => root.Properties.ContainsKey(name) ? Integer(root, name) : null;

    private static ContentDigest Digest(CanonicalJsonObject root, string name)
    {
        try { return ContentDigest.Parse(String(root, name)); }
        catch (Exception exception) when (exception is ArgumentException or FormatException) { throw Rule("Execution digest is invalid.", exception); }
    }

    private static DateTimeOffset Timestamp(CanonicalJsonObject root, string name)
    {
        var value = String(root, name);
        CanonicalTimestamp.ValidateCanonicalUtc(value);
        return DateTimeOffset.ParseExact(
            value, CanonicalTimestamp.DefaultUtcFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private static T? OptionalObject<T>(CanonicalJsonObject root, string name, Func<CanonicalJsonObject, T> parser)
        where T : class => root.Properties.ContainsKey(name) ? parser(Object(root, name)) : null;

    private static CanonicalJsonValue Value(CanonicalJsonObject root, string name) =>
        root.Properties.TryGetValue(name, out var value) ? value : throw Rule($"Execution canonical field '{name}' is required.");

    private static void RequireDigest(ContentDigest actual, ContentDigest expected, string kind)
    {
        if (actual != expected) throw Rule($"{kind} did not reproduce the expected digest.");
    }

    private static WorkflowExecutionRuleException Rule(string message, Exception? inner = null) =>
        new(WorkflowExecutionErrorCodes.UnverifiedAuthority, inner is null ? message : $"{message} {inner.Message}");
}
