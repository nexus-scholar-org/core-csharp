# ADR 0009: Portable Bundle and Artifact Contract

Status: Accepted

Date: 2026-06-27

## Context

Gate 6 needs a local, portable bundle and artifact contract before source implementation. The current `NexusScholar.Bundles` and `NexusScholar.Artifacts` code is a thin scaffold and is not sufficient as an authoritative review-bundle contract.

`ADR 0002` already defines canonical JSON, SHA-256 rendering, `raw-artifact-bytes`, and `bundle-manifest` digest scopes. `ADR 0003` and `ADR 0004` define approved protocol versions and approval authority. `ADR 0005` and `ADR 0006` define workflow template and compiled workflow records. `ADR 0007` defines local shared scientific identity and unresolved no-id candidate handling. `ADR 0008` defines local provenance event records.

The blueprint bundle and artifact materials are discovery inputs only. They are not adopted as authoritative local specifications in Gate 6. Gate 6 also does not claim PHP compatibility.

## Decision

### 1. Bundle manifest schema and version

The local Gate 6 review-bundle manifest schema id is:

```text
nexus.review-bundle.manifest
```

The initial local schema version is:

```text
1.0.0
```

Any semantic change to required sections, artifact entry fields, digest material, import validation, tamper categories, or local equality rules requires a new schema version.

### 2. Bundle manifest identity

A bundle manifest must carry:

- `bundle_id`
- `bundle_kind`
- `schema_id`
- `schema_version`
- `created_at`
- `created_by`
- `protocol_binding`
- `artifacts`
- `required_schemas`

`bundle_kind` is `review-bundle` for Gate 6. `bundle_id` is an explicit stable id supplied by the export command or fixture harness. It is not generated during canonical serialization.

`created_at` is included in the manifest only after a fixed clock value is supplied. It is part of the manifest digest once present.

### 3. Bundle manifest digest input

The bundle manifest digest uses `DigestScope.BundleManifest`.

The digest envelope is:

- `scope = bundle-manifest`
- `schemaId = nexus.review-bundle.manifest`
- `schemaVersion = 1.0.0`
- `content = canonical manifest content`

The digest input includes all required and optional manifest sections that affect scientific reconstruction, artifact verification, protocol binding, workflow binding, provenance binding, shared-identity membership, unresolved candidates, and import safety.

The digest input excludes the manifest digest value being computed.

### 4. Artifact entry shape

Each manifest artifact entry must carry:

- `artifact_ref`
- `logical_path`
- `artifact_kind`
- `media_type`
- `size_bytes`
- `raw_byte_digest`
- `schema_id`
- `schema_version`

An artifact entry may also carry:

- `source_record_digest`
- `produced_by_workflow_node`
- `provenance_event_id`
- `provenance_event_digest`
- `required_for`

`raw_byte_digest` must use `DigestScope.RawArtifactBytes`.

### 5. Logical artifact path rules

Artifact paths are logical bundle paths, not local filesystem paths.

Rules:

- path separator is `/`;
- backslash is invalid;
- absolute paths are invalid;
- drive-letter paths are invalid;
- URI paths are invalid;
- traversal segments are invalid;
- empty path segments are invalid;
- `.` path segments are invalid;
- leading or trailing `/` is invalid;
- paths are compared with ordinal, case-sensitive equality;
- duplicate normalized paths are rejected.

The implementation must never use a manifest logical path as a direct filesystem write path without import-target normalization and overwrite checks.

### 6. Raw artifact byte digest rules

Artifact raw-byte digests are SHA-256 digests over the exact artifact bytes.

Line endings, encodings, compression, container metadata, local path names, and extracted filename casing are not normalized before computing `raw-artifact-bytes` digests. If an artifact is text, its exact stored bytes are the digest input.

### 7. Manifest checksum rules

The manifest records each artifact's `raw_byte_digest` and `size_bytes`. Verification recomputes both from supplied artifact bytes and rejects mismatches.

The manifest digest verifies the manifest content, including artifact entries and artifact checksums. It does not verify the archive container, compression method, ZIP entry metadata, or transport headers.

### 8. Required and optional local sections

Required manifest sections:

- `manifest_identity`
- `protocol_binding`
- `artifacts`
- `required_schemas`
- `verification_policy`

Optional manifest sections:

- `workflow_binding`
- `provenance_bindings`
- `shared_identity_membership`
- `unresolved_candidates`
- `notes`

`notes` are operator-authored manifest metadata only. They are not a place for generated narrative reports, wiki content, verification reports, tamper reports, provider logs, or cache projections. Generated narrative content remains outside the bundle manifest digest unless it is deliberately exported as an artifact with a raw-byte digest.

Optional sections are omitted when absent. `null` is not equivalent to omitted.

### 9. Protocol approved-version binding

Every Gate 6 review bundle binds to exactly one approved protocol version.

The protocol binding must include:

- `protocol_id`
- `protocol_version_id`
- `version_number`
- `status = approved`
- `protocol_content_digest`

`protocol_content_digest` must be the digest produced by the referenced protocol version's `protocol-content` digest envelope. A syntactically valid `sha256:*` value is not sufficient during verification; the importer must compare the binding against a known protocol-content envelope digest for the referenced protocol version id. Draft, ready-for-review, withdrawn, or superseded protocol versions cannot satisfy the binding unless a later explicit import policy accepts superseded versions for historical replay.

### 10. Workflow binding

When workflow content is included, the workflow binding must include:

- `workflow_id`
- `workflow_definition_digest`
- `template_id`
- `template_version`
- `template_digest`
- `bound_protocol_version_id`
- `bound_protocol_content_digest`

The bound protocol fields must match the manifest `protocol_binding`. Gate 6 records workflow definitions and compiled workflow identity only. It does not implement workflow execution.

### 11. Provenance event binding

When provenance events are included, each binding must include:

- `event_id`
- `event_digest`
- `activity_kind`
- `recorded_at`
- `actor_id`

`event_digest` must be the digest produced by the referenced event's `provenance-event` digest envelope. A syntactically valid `sha256:*` value is not sufficient during verification; the importer must compare each binding against a known provenance-event envelope digest for the referenced event id. Gate 6 verifies provenance references by id and digest only. It does not replay the provenance ledger or infer omitted events.

### 12. Shared identity and corpus membership treatment

When shared-identity membership is included, the manifest uses the local `ADR 0007` identity rules:

- stable work ids are normalized before inclusion;
- membership identity uses stable identifier overlap, not title-only matching;
- runtime object identity is never exported or imported as scientific identity;
- no PHP compatibility is claimed.

Canonical membership entries must contain at least one stable identifier. Membership entries are deterministically ordered by primary id precedence and canonical id value.

### 13. Unresolved no-id candidates

No-id works may be carried only in `unresolved_candidates`.

Unresolved candidates must include source context sufficient for audit replay, but they are not canonical corpus membership identity and cannot satisfy stable snapshot membership equality. They may round-trip as staged import candidates, not as deduplicated works.

### 14. Local snapshot equality for Gate 6

Gate 6 defines local bundle round-trip equality only.

Two local bundle round trips are equal when all of the following match:

- manifest digest;
- bundle schema id and version;
- protocol binding;
- optional workflow binding when present;
- optional provenance bindings when present;
- artifact logical paths, sizes, media types, schema refs, and raw-byte digests;
- stable shared-identity membership entries when present;
- unresolved candidate records when present.

This is not a general corpus snapshot equality rule and does not resolve Search, Deduplication, Screening, lock-state, release, or PHP snapshot semantics.

### 15. Verification result shape

Bundle verification returns an immutable result containing:

- `is_valid`
- `manifest_digest`
- `verified_artifacts`
- `errors`
- `warnings`

`verified_artifacts`, `errors`, and `warnings` are immutable snapshots. Verification does not mutate manifests or artifact entries.

### 16. Tamper report shape and categories

Tamper findings use stable categories. Gate 6 must support at least:

- `invalid-manifest`
- `invalid-manifest-digest`
- `unsupported-required-schema`
- `missing-required-section`
- `invalid-artifact-path`
- `duplicate-artifact-path`
- `invalid-artifact-digest`
- `negative-artifact-size`
- `missing-artifact`
- `artifact-size-mismatch`
- `checksum-mismatch`
- `stale-manifest-digest`
- `destructive-overwrite`
- `invalid-protocol-binding`
- `invalid-workflow-binding`
- `invalid-provenance-binding`

Reports may include human-readable messages, but tests and fixtures must assert stable categories rather than prose.

### 17. Import validation

Import is staged and all-or-nothing:

1. parse the manifest;
2. validate schema id and version;
3. validate required sections;
4. validate deterministic ordering;
5. recompute the manifest digest;
6. verify all required schemas are supported locally;
7. verify every artifact logical path;
8. recompute artifact byte digests and sizes;
9. verify protocol, workflow, provenance, and shared-identity bindings;
10. check target import locations for destructive overwrite risk;
11. commit only after every validation step succeeds.

No partial import is allowed after a failed verification step.

### 18. Destructive overwrite policy

Gate 6 rejects destructive overwrite attempts by default.

An import target is destructive when the target already contains a record or artifact with the same local key or logical path and a different digest. A later ADR may define an explicit safe replacement policy, but Gate 6 has no implicit overwrite mode.

### 19. Deterministic ordering

Canonical manifest arrays must use deterministic ordering:

- `artifacts`: by `logical_path`, ordinal;
- `required_schemas`: by `schema_id`, then `schema_version`, ordinal;
- `provenance_bindings`: by `event_id`, then `event_digest.value`, ordinal;
- `shared_identity_membership`: by primary id namespace precedence, then canonical primary id value;
- `unresolved_candidates`: by source context digest or stable fixture id, ordinal.

Input order must not affect the manifest digest when the semantic content is identical.

### 20. Outside bundle digest

The bundle manifest digest excludes:

- the manifest digest value being computed;
- ZIP or archive container metadata;
- compression settings;
- transport headers;
- local filesystem paths;
- local machine, process, or user account names;
- verification reports;
- tamper reports;
- import logs;
- detached signatures;
- encryption envelopes;
- cache state;
- wiki pages;
- generated narrative reports;
- provider/network request logs unless they are explicitly exported as artifacts with raw-byte digests.

### 21. Future work and non-claims

Future work:

- source implementation of the Gate 6 local contract;
- generated Gate 6 conformance fixtures;
- import/export CLI or API surfaces;
- persistence schema;
- detached signatures;
- encryption;
- cloud sync;
- blueprint conformance;
- PHP compatibility fixtures and comparators;
- bundle support for Search, Deduplication, Screening, Citation Network, Full Text, and Reporting ports;
- AI governance and plugin runtime integration.

## Alternatives Considered

### Adopt the blueprint bundle schema directly

Rejected for Gate 6. The blueprint bundle materials are discovery inputs and still drift from local accepted ADRs and current C# implementation. Adopting them silently would violate `CF-005`.

### Treat archive bytes as the bundle digest

Rejected. Archive bytes include compression and container metadata that are not scientific content. Gate 6 needs a manifest digest over canonical scientific reconstruction material.

### Treat unresolved no-id works as canonical membership

Rejected. `ADR 0007` forbids runtime object identity and title-only fallback as scientific identity. No-id candidates can be carried for audit/staging, but cannot become canonical membership identity.

### Allow import overwrite by default

Rejected. Silent overwrite would violate append-only audit expectations and could replace previously verified scientific content without an explicit human decision.

## Consequences

Positive:

- `CF-002` is resolved for local Gate 6 planning: local manifest, artifact, digest, tamper, and import-safety semantics are now defined.
- Gate 6 implementation can proceed without adopting blueprint bundle authority or PHP compatibility.
- Bundle verification can produce stable fixture-backed tamper categories.
- Artifact byte identity is separated from manifest identity.

Negative:

- Blueprint bundle conformance remains unclaimed.
- PHP compatibility remains unclaimed.
- Full corpus snapshot equality remains unresolved outside local Gate 6 bundle round-trip equality.
- Import/export persistence, CLI, API, and cloud surfaces remain future work.

## Migration Effect

Existing `NexusScholar.Bundles` and `NexusScholar.Artifacts` scaffolds must be revised during Gate 6 implementation to match this ADR. The current scaffold schema string and artifact shape are not authoritative.

Any pre-ADR bundle or artifact fixture must be treated as discovery material unless regenerated under this contract.

## Fixture Effect

Gate 6 fixtures must cover:

- `artifact-raw-byte-digest.json`
- `artifact-manifest-entry.json`
- `bundle-manifest-minimal.json`
- `bundle-manifest-with-protocol-workflow-provenance.json`
- `bundle-manifest-digest-stable.json`
- `bundle-roundtrip-local-equivalence.json`

Negative fixtures must cover:

- invalid artifact digest;
- negative artifact size;
- absolute artifact path;
- traversal artifact path;
- duplicate artifact path;
- missing artifact;
- checksum mismatch;
- unsupported required schema;
- stale manifest digest;
- destructive overwrite attempt.

Fixtures are local conformance fixtures, not PHP-generated goldens.

## Conflict Effect

`CF-002` is resolved for local Gate 6 bundle and artifact planning. Implementation and evidence remain pending.

`CF-014` is narrowed for local Gate 6 bundle round-trip equality and import safety. Broader corpus snapshot equality remains unresolved for future corpus/Search/Deduplication/Screening gates.

`CF-005` remains open. Blueprint materials remain discovery inputs only.

## Reversal Conditions

This ADR should be revised if:

1. later blueprint adoption becomes authoritative through a dedicated ADR and conflicts with this local contract;
2. PHP-generated fixture evidence requires a different compatibility rule and the repository chooses to claim PHP compatibility;
3. a future persistence or cloud-sync ADR needs a stronger import replacement policy;
4. corpus snapshot gates define a broader equality model that requires a versioned migration of Gate 6 membership sections.

## Explicit Claims Not Made

- no source implementation
- no generated fixtures
- no blueprint conformance
- no PHP compatibility
- no PHP-generated fixtures
- no persistence schema
- no API, UI, or cloud sync
- no provider/network behavior
- no workflow execution engine
- no plugin runtime
- no AI governance parity
- no general corpus snapshot equality
