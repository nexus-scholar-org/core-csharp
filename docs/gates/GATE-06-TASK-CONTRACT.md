# Gate 6 Task Contract: Bundle and Artifact Implementation

Mode: implementation may proceed only against `ADR 0009`. Do not claim blueprint conformance or PHP compatibility.

Branch:

```text
cdx/gate-6-bundle-planning
```

## Objective

Implement the local portable bundle and artifact contract accepted in `docs/adr/0009-portable-bundle-and-artifact-contract.md`.

## Read First

- `AGENTS.md`
- `PLANS.md`
- `docs/adr/0001-source-of-truth-and-porting.md`
- `docs/adr/0002-canonical-json-and-digests.md`
- `docs/adr/0003-protocol-record-contract.md`
- `docs/adr/0004-protocol-approval-semantics.md`
- `docs/adr/0005-workflow-template-contract.md`
- `docs/adr/0006-workflow-compiler-semantics.md`
- `docs/adr/0007-shared-scientific-identity.md`
- `docs/adr/0008-provenance-ledger.md`
- `docs/gates/GATE-06.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`
- `docs/discovery/BLUEPRINT-AUDIT.md`
- `docs/scientific-invariants/PRODUCT-LAWS.md`
- `src/NexusScholar.Artifacts/**`
- `src/NexusScholar.Bundles/**`

## Allowed Paths

- `docs/adr/0009-portable-bundle-and-artifact-contract.md`
- `docs/gates/GATE-06.md`
- `docs/gates/GATE-06-TASK-CONTRACT.md`
- `docs/gates/GATE-06-EVIDENCE.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`
- `src/NexusScholar.Artifacts/**`
- `src/NexusScholar.Bundles/**`
- `tests/NexusScholar.Core.Tests/**`
- `tests/NexusScholar.Architecture.Tests/**`
- `tests/NexusScholar.Conformance.Tests/**`
- `fixtures/conformance/artifacts/**`
- `fixtures/conformance/bundles/**`

## Forbidden Paths

- `specs/**`
- PHP reference repo
- persistence, EF Core, SQLite, filesystem database adapters
- API/UI/cloud sync
- provider/network calls
- Search, Deduplication, Screening, Citation Network, Full Text, or Reporting ports
- plugin host/runtime
- AI governance behavior
- workflow execution engine

## Behavior To Implement

Implement the local contract from `ADR 0009`:

- create artifact manifest entries with logical path, artifact kind, media type, byte size, raw-byte digest, schema id, and schema version;
- compute artifact raw-byte digests with `DigestScope.RawArtifactBytes`;
- validate logical paths and reject absolute, drive-letter, URI, traversal, empty-segment, dot-segment, leading slash, trailing slash, backslash, and duplicate normalized paths;
- create deterministic review-bundle manifests with schema id `nexus.review-bundle.manifest` and schema version `1.0.0`;
- compute manifest digests with `DigestScope.BundleManifest`;
- preserve approved protocol binding by protocol id, protocol version id, version number, status, and `protocol-content` digest;
- preserve workflow binding by workflow id, workflow-definition digest, template identity, and bound protocol digest when included;
- preserve provenance event bindings by event id and `provenance-event` digest when included;
- preserve shared-identity membership using `ADR 0007` stable identifier rules when included;
- carry no-id works only as unresolved candidates, not canonical membership identity;
- expose immutable verification results and stable tamper categories;
- verify artifact size and raw-byte digest before import;
- reject unsupported required schemas;
- reject stale manifest digest;
- reject destructive overwrite attempts;
- import only after all validation succeeds;
- keep local round-trip equality limited to the `ADR 0009` Gate 6 rule.

## Fixture IDs To Plan

Positive:

- `artifact-raw-byte-digest.json`
- `artifact-manifest-entry.json`
- `bundle-manifest-minimal.json`
- `bundle-manifest-with-protocol-workflow-provenance.json`
- `bundle-manifest-digest-stable.json`
- `bundle-roundtrip-local-equivalence.json`

Negative:

- `artifact-invalid-digest.json`
- `artifact-negative-size.json`
- `artifact-forbidden-path-absolute.json`
- `artifact-forbidden-path-traversal.json`
- `bundle-duplicate-artifact-path.json`
- `bundle-missing-artifact.json`
- `bundle-checksum-mismatch.json`
- `bundle-unsupported-required-schema.json`
- `bundle-stale-manifest-digest.json`
- `bundle-destructive-overwrite-reject.json`

## Non-Claims

Do not claim:

- blueprint conformance;
- PHP compatibility;
- PHP-generated fixtures;
- provider behavior;
- persistence, API, UI, or cloud sync;
- Search, Deduplication, Screening, Citation Network, Full Text, or Reporting ports;
- workflow execution;
- plugin runtime;
- AI governance parity.

## Finish With

- ADR summary;
- `CF-002` status;
- `CF-014` status;
- fixture consequences;
- implementation readiness: yes/no;
- explicit claims not made.
