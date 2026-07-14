using System.Globalization;
using System.Text.Json;
using NexusScholar.CorpusSnapshots;
using NexusScholar.Deduplication;
using NexusScholar.Kernel;
using NexusScholar.Provenance;

namespace NexusScholar.ResearchWorkspace;

public static class ResearchWorkspaceAuthorityArtifacts
{
    public static byte[] SerializePolicyCanonicalRecord(VerifiedDeduplicationAuthorityPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var content = CanonicalJsonValue.DeepClone(policy.PolicyDigestEnvelope.Content) as CanonicalJsonObject
            ?? throw new InvalidOperationException("Policy digest material must be an object.");
        content.Add("policy_digest", policy.PolicyDigest.ToString());

        return CanonicalJsonSerializer.SerializeToUtf8Bytes(content);
    }

    public static byte[] SerializeSnapshotCanonicalRecord(VerifiedCorpusSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var content = CanonicalJsonValue.DeepClone(snapshot.RecordDigestEnvelope.Content) as CanonicalJsonObject
            ?? throw new InvalidOperationException("Snapshot record digest material must be an object.");
        content.Add("record_digest", snapshot.RecordDigest.ToString());

        return CanonicalJsonSerializer.SerializeToUtf8Bytes(content);
    }

    public static byte[] SerializeResearchEventCanonicalRecord(ResearchEvent researchEvent)
    {
        ArgumentNullException.ThrowIfNull(researchEvent);

        var content = new CanonicalJsonObject().Add("event_id", researchEvent.EventId.ToString())
            .Add("agent", researchEvent.Agent.ToCanonicalJson())
            .Add("activity", researchEvent.Activity.ToCanonicalJson())
            .AddTimestamp("occurred_at", researchEvent.OccurredAt)
            .Add("subject", researchEvent.Subject.ToCanonicalJson())
            .Add("inputs", CanonicalJsonValue.Array(researchEvent.Inputs.Select(input => input.ToCanonicalJson()).ToArray()))
            .Add("outputs", CanonicalJsonValue.Array(researchEvent.Outputs.Select(output => output.ToCanonicalJson()).ToArray()));

        if (researchEvent.ProtocolBinding is not null)
        {
            content.Add("protocol_binding", researchEvent.ProtocolBinding.ToCanonicalJson());
        }

        if (researchEvent.WorkflowBinding is not null)
        {
            content.Add("workflow_binding", researchEvent.WorkflowBinding.ToCanonicalJson());
        }

        content.Add("event_digest", researchEvent.EventDigest.ToString());

        return CanonicalJsonSerializer.SerializeToUtf8Bytes(content);
    }

    internal static CanonicalJsonObject ParseCanonicalPolicyRecord(byte[] canonicalPolicyRecord)
    {
        var canonical = ParseCanonicalObject(canonicalPolicyRecord);
        if (!canonical.Contains("policy_id"))
        {
            throw new InvalidOperationException("Authority policy record is missing required policy identifier.");
        }

        return canonical;
    }

    internal static CanonicalJsonObject ParseCanonicalSnapshotRecord(byte[] canonicalSnapshotRecord)
    {
        var canonical = ParseCanonicalObject(canonicalSnapshotRecord);
        if (!canonical.Contains("snapshot_id"))
        {
            throw new InvalidOperationException("Corpus snapshot record is missing required snapshot identifier.");
        }

        return canonical;
    }

    internal static CanonicalJsonObject ParseCanonicalResearchEventRecord(byte[] canonicalEventRecord)
    {
        var canonical = ParseCanonicalObject(canonicalEventRecord);
        if (!canonical.Contains("event_id"))
        {
            throw new InvalidOperationException("Research event record is missing required event identifier.");
        }

        return canonical;
    }

    public static VerifiedDeduplicationAuthorityPolicy VerifyPolicyCanonicalRecord(
        byte[] canonicalPolicyRecord)
    {
        var parsed = ParseCanonicalPolicyRecord(canonicalPolicyRecord);

        var unverified = new UnverifiedDeduplicationAuthorityPolicy(
            SchemaId: RequireString(parsed, "schema_id"),
            SchemaVersion: RequireString(parsed, "schema_version"),
            AuthoritySourceKind: RequireString(parsed, "authority_source_kind"),
            PolicyId: RequireString(parsed, "policy_id"),
            PolicyVersion: RequireString(parsed, "policy_version"),
            AuthorizedActorRoles: RequireArray(parsed, "authorized_actor_roles")
                .Select(ParseActorRole)
                .ToArray(),
            AllowedActions: RequireArray(parsed, "allowed_actions").Select(RequireString).ToArray(),
            ReasonCodesByAction: RequireArray(parsed, "reason_codes_by_action").Select(ParseReasonCodeGroup).ToArray(),
            RequiresRationale: RequireBoolean(parsed, "requires_rationale"),
            IssuedByActorId: RequireString(parsed, "issued_by_actor_id"),
            IssuedByRole: RequireString(parsed, "issued_by_role"),
            IssuedAt: ParseCanonicalTimestamp(RequireString(parsed, "issued_at")),
            SupersedesPolicyId: TryGetString(parsed, "supersedes_policy_id"),
            SupersedesPolicyDigest: TryGetDigest(parsed, "supersedes_policy_digest"),
            PolicyDigest: ParseDigest(RequireString(parsed, "policy_digest")));

        return DeduplicationAuthorityPolicy.RehydratePolicyMaterial(unverified);
    }

    public static VerifiedCorpusSnapshot VerifySnapshotCanonicalRecord(
        byte[] canonicalSnapshotRecord,
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedDeduplicationAuthorityPolicy policy)
    {
        var parsed = ParseCanonicalSnapshotRecord(canonicalSnapshotRecord);

        var decisionReferences = RequireArray(parsed, "decision_references")
            .Select(item => ParseDecisionReference(item))
            .ToArray();
        var groups = RequireArray(parsed, "groups")
            .Select(ParseGroup)
            .ToArray();
        var unresolvedCandidates = RequireArray(parsed, "unresolved_candidates")
            .Select(ParseUnresolvedCandidate)
            .ToArray();
        var invalidations = RequireArray(parsed, "invalidation_references")
            .Select(ParseInvalidationReference)
            .ToArray();

        var unverified = new UnverifiedCorpusSnapshot(
            SchemaId: RequireString(parsed, "schema_id"),
            SchemaVersion: RequireString(parsed, "schema_version"),
            SnapshotId: RequireString(parsed, "snapshot_id"),
            SourceResultId: RequireString(parsed, "source_result_id"),
            SourceResultDigest: ParseDigest(RequireString(parsed, "source_result_digest")),
            DecisionReferences: decisionReferences,
            DecisionSetDigest: ParseDigest(RequireString(parsed, "decision_set_digest")),
            Groups: groups,
            UnresolvedCandidates: unresolvedCandidates,
            CreatedByActorId: RequireString(parsed, "created_by_actor_id"),
            CreatedByRole: RequireString(parsed, "created_by_role"),
            AuthoritySourceId: RequireString(parsed, "authority_source_id"),
            AuthoritySourceDigest: ParseDigest(RequireString(parsed, "authority_source_digest")),
            CreatedAt: ParseCanonicalTimestamp(RequireString(parsed, "created_at")),
            SupersedesSnapshotId: TryGetString(parsed, "supersedes_snapshot_id"),
            SupersedesSnapshotRecordDigest: TryGetDigest(parsed, "supersedes_snapshot_record_digest"),
            InvalidationReferences: invalidations,
            ContentDigest: ParseDigest(RequireString(parsed, "content_digest")),
            RecordDigest: ParseDigest(RequireString(parsed, "record_digest")));

        return CorpusSnapshotService.Rehydrate(unverified, sourceResult, policy);
    }

    public static ResearchEvent VerifyResearchEventCanonicalRecord(byte[] canonicalEventRecord)
    {
        var parsed = ParseCanonicalResearchEventRecord(canonicalEventRecord);

        var eventId = EntityId<ProvenanceEventTag>.From(
            Guid.Parse(RequireString(parsed, "event_id"), CultureInfo.InvariantCulture));
        var idGenerator = new SingleGuidIdGenerator(eventId.Value);
        var occurredAt = ParseCanonicalTimestamp(RequireString(parsed, "occurred_at"));
        var clock = new FixedClock(occurredAt);

        var agent = ParseAgent(RequireObject(parsed, "agent"));
        var activity = ParseActivity(RequireObject(parsed, "activity"));
        var subject = ParseEntityRef(RequireObject(parsed, "subject"));
        var inputs = RequireArray(parsed, "inputs").Select(item => ParseEntityRef(ParseObject(item))).ToArray();
        var outputs = RequireArray(parsed, "outputs").Select(item => ParseEntityRef(ParseObject(item))).ToArray();

        var protocolBinding = parsed.Properties.ContainsKey("protocol_binding")
            ? ParseProtocolBinding(RequireObject(parsed, "protocol_binding"))
            : null;

        var workflowBinding = parsed.Properties.ContainsKey("workflow_binding")
            ? ParseWorkflowBinding(RequireObject(parsed, "workflow_binding"))
            : null;

        var eventRecord = ResearchEventFactory.Create(
            idGenerator,
            clock,
            activity,
            subject,
            agent,
            inputs,
            outputs,
            protocolBinding,
            workflowBinding);

        if (eventRecord.EventDigest != ParseDigest(RequireString(parsed, "event_digest")))
        {
            throw new ProvenanceRuleException(
                ProvenanceErrorCodes.StaleEventDigest,
                "Research event digest does not match persisted event digest.");
        }

        return eventRecord;
    }

    private static CanonicalJsonObject ParseCanonicalObject(byte[] canonicalText)
    {
        ArgumentNullException.ThrowIfNull(canonicalText);

        var parsed = CanonicalJsonValue.FromJsonElement(JsonDocument.Parse(canonicalText).RootElement);
        if (parsed is not CanonicalJsonObject record)
        {
            throw new InvalidOperationException("Canonical records must be JSON objects.");
        }

        var canonicalBytes = CanonicalJsonSerializer.SerializeToUtf8Bytes(record);
        if (!canonicalText.SequenceEqual(canonicalBytes))
        {
            throw new InvalidOperationException("Canonical record bytes are not in canonical form.");
        }

        return record;
    }

    private static ContentDigest ParseDigest(string value)
    {
        return ContentDigest.Parse(value);
    }

    private static ContentDigest? TryGetDigest(CanonicalJsonObject root, string propertyName)
    {
        return root.Properties.TryGetValue(propertyName, out var value)
            ? value switch
            {
                CanonicalJsonString { } digestValue => ParseDigest(digestValue.Value),
                _ => null
            }
            : null;
    }

    private static string? TryGetString(CanonicalJsonObject root, string propertyName)
    {
        return root.Properties.TryGetValue(propertyName, out var value)
            ? value switch
            {
                CanonicalJsonString { } textValue => textValue.Value,
                _ => null
            }
            : null;
    }

    private static string RequireString(CanonicalJsonObject root, string propertyName) =>
        root.Properties.TryGetValue(propertyName, out var value)
            ? value is CanonicalJsonString textValue
                ? textValue.Value
                : throw new InvalidOperationException($"Property '{propertyName}' must be a JSON string.")
            : throw new InvalidOperationException($"Property '{propertyName}' is required.");

    private static bool RequireBoolean(CanonicalJsonObject root, string propertyName) =>
        root.Properties.TryGetValue(propertyName, out var value)
            ? value is CanonicalJsonBoolean booleanValue
                ? booleanValue.Value
                : throw new InvalidOperationException($"Property '{propertyName}' must be a JSON boolean.")
            : throw new InvalidOperationException($"Property '{propertyName}' is required.");

    private static IReadOnlyList<CanonicalJsonValue> RequireArray(CanonicalJsonObject root, string propertyName) =>
        root.Properties.TryGetValue(propertyName, out var value)
            ? value is CanonicalJsonArray array
                ? array.Items
                : throw new InvalidOperationException($"Property '{propertyName}' must be a JSON array.")
            : throw new InvalidOperationException($"Property '{propertyName}' is required.");

    private static CanonicalJsonObject RequireObject(CanonicalJsonObject root, string propertyName) =>
        root.Properties.TryGetValue(propertyName, out var value)
            ? value is CanonicalJsonObject nested
                ? nested
                : throw new InvalidOperationException($"Property '{propertyName}' must be a JSON object.")
            : throw new InvalidOperationException($"Property '{propertyName}' is required.");

    private static CanonicalJsonValue RequireValue(CanonicalJsonObject root, string propertyName) =>
        root.Properties.TryGetValue(propertyName, out var value)
            ? value
            : throw new InvalidOperationException($"Property '{propertyName}' is required.");

    private static CanonicalJsonObject ParseObject(CanonicalJsonValue value) =>
        value is CanonicalJsonObject obj
            ? obj
            : throw new InvalidOperationException("Expected a JSON object.");

    private static string RequireString(CanonicalJsonValue value) =>
        value is CanonicalJsonString textValue
            ? textValue.Value
            : throw new InvalidOperationException("Expected a JSON string.");

    private static DateTimeOffset ParseCanonicalTimestamp(string value)
    {
        CanonicalTimestamp.ValidateCanonicalUtc(value);
        return DateTimeOffset.ParseExact(
            value,
            CanonicalTimestamp.DefaultUtcFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private static DeduplicationAuthorityPolicyActorRole ParseActorRole(CanonicalJsonValue value)
    {
        var item = ParseObject(value);

        return new DeduplicationAuthorityPolicyActorRole(
            RequireString(item, "actor_id"),
            RequireString(item, "role"),
            RequireString(item, "subject_kind"));
    }

    private static DeduplicationAuthorityPolicyReasonGroup ParseReasonCodeGroup(CanonicalJsonValue value)
    {
        var item = ParseObject(value);

        return new DeduplicationAuthorityPolicyReasonGroup(
            RequireString(item, "action"),
            RequireArray(item, "reason_codes").Select(RequireString).ToArray());
    }

    private static CorpusSnapshotDecisionReference ParseDecisionReference(CanonicalJsonValue value)
    {
        var item = ParseObject(value);

        return new CorpusSnapshotDecisionReference(
            RequireString(item, "decision_id"),
            ParseDigest(RequireString(item, "decision_digest")));
    }

    private static CorpusSnapshotGroup ParseGroup(CanonicalJsonValue value)
    {
        var group = ParseObject(value);

        var references = RequireArray(group, "evidence_references").Select(ParseEvidenceReference).ToArray();
        var memberCandidateIds = RequireArray(group, "member_candidate_ids").Select(RequireString).ToArray();

        return new CorpusSnapshotGroup(
            RequireString(group, "group_id"),
            RequireString(group, "representative_candidate_id"),
            memberCandidateIds,
            references);
    }

    private static CorpusSnapshotEvidenceReference ParseEvidenceReference(CanonicalJsonValue value)
    {
        var item = ParseObject(value);

        return new CorpusSnapshotEvidenceReference(
            RequireString(item, "kind"),
            RequireString(item, "evidence_id"),
            RequireString(item, "digest_scope"),
            ParseDigest(RequireString(item, "digest")));
    }

    private static CorpusSnapshotUnresolvedCandidate ParseUnresolvedCandidate(CanonicalJsonValue value)
    {
        var item = ParseObject(value);

        var rawSightings = RequireArray(item, "raw_sighting_references").Select(RequireString).ToArray();

        return new CorpusSnapshotUnresolvedCandidate(
            RequireString(item, "candidate_id"),
            RequireString(item, "unresolved_reason"),
            rawSightings,
            ParseDigest(RequireString(item, "candidate_content_digest")));
    }

    private static CorpusSnapshotInvalidationReference ParseInvalidationReference(CanonicalJsonValue value)
    {
        var item = ParseObject(value);

        return new CorpusSnapshotInvalidationReference(
            RequireString(item, "record_kind"),
            RequireString(item, "record_id"),
            ParseDigest(RequireString(item, "record_digest")));
    }

    private static ProvenanceActivity ParseActivity(CanonicalJsonObject value) => new(
        ActivityId: RequireString(value, "activity_id"),
        ActivityLabel: RequireString(value, "activity_label"),
        RequiresActor: RequireBoolean(value, "requires_actor"),
        RequiresInput: RequireBoolean(value, "requires_input"),
        RequiresOutput: RequireBoolean(value, "requires_output"));

    private static ProvenanceAgent ParseAgent(CanonicalJsonObject value)
    {
        var displayName = TryGetString(value, "display_name");

        return displayName is null
            ? new ProvenanceAgent(RequireString(value, "agent_id"), RequireString(value, "agent_kind"))
            : new ProvenanceAgent(RequireString(value, "agent_id"), RequireString(value, "agent_kind"), displayName);
    }

    private static ProvenanceEntityRef ParseEntityRef(CanonicalJsonObject value)
    {
        var digest = TryGetDigest(value, "content_digest");

        return digest is null
            ? new ProvenanceEntityRef(RequireString(value, "entity_kind"), RequireString(value, "entity_id"))
            : new ProvenanceEntityRef(RequireString(value, "entity_kind"), RequireString(value, "entity_id"), digest.Value);
    }

    private static ProvenanceProtocolBinding ParseProtocolBinding(CanonicalJsonObject value) => new(
        ProtocolId: RequireString(value, "protocol_id"),
        ProtocolVersionId: RequireString(value, "protocol_version_id"),
        ProtocolVersionNumber: ParseProtocolVersionNumber(RequireValue(value, "protocol_version_number")),
        ProtocolContentDigest: ParseDigest(RequireString(value, "protocol_content_digest")));

    private static ProvenanceWorkflowBinding ParseWorkflowBinding(CanonicalJsonObject value)
    {
        var nodeId = TryGetString(value, "workflow_node_id");

        return new ProvenanceWorkflowBinding(
            WorkflowId: RequireString(value, "workflow_id"),
            WorkflowDigest: ParseDigest(RequireString(value, "workflow_digest")),
            WorkflowNodeId: nodeId);
    }

    private sealed class SingleGuidIdGenerator(Guid value) : IIdGenerator
    {
        private readonly Guid _value = value;

        public Guid NewId() => _value;
    }

    private sealed class FixedClock(DateTimeOffset value) : IClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }

    private static int ParseProtocolVersionNumber(CanonicalJsonValue value)
    {
        return value switch
        {
            CanonicalJsonNumber number => int.Parse(number.Value, CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("Property 'protocol_version_number' must be a JSON number.")
        };
    }
}
