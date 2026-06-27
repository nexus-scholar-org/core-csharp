# Gate 9 Deduplication

Status: local implementation implemented on `cdx/gate-9-dedup-local`.

## Goal

Implement local C# deduplication for deterministic, trace-bound evidence clustering and representative election against `ADR 0012`, while keeping Search and App/Web projections as consumers only.

## Sources Read

- `AGENTS.md`
- `docs/adr/0001-source-of-truth-and-porting.md`
- `docs/adr/0002-canonical-json-and-digests.md`
- `docs/adr/0007-shared-scientific-identity.md`
- `docs/adr/0010-search-trace-and-plan-contract.md`
- `docs/adr/0011-search-import-source-contract.md`
- `docs/adr/0012-deduplication-evidence-and-cluster-contract.md`
- `docs/port/php-deduplication-behavior.md`
- `docs/port/php-deduplication-fixture-plan.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`
- PHP reconnaissance and test fixtures (for behavior parity planning only)

## Allowed Paths

- `src/NexusScholar.Deduplication/**`
- `src/NexusScholar.Search/**` (only existing trace/input types)
- `src/NexusScholar.Kernel/**` (only if a primitive is reusable)
- `tests/NexusScholar.Core.Tests/**`
- `tests/NexusScholar.Architecture.Tests/**`
- `tests/NexusScholar.Conformance.Tests/**`
- `fixtures/conformance/deduplication/**`
- `docs/gates/GATE-09-DEDUP.md`
- `docs/gates/GATE-09-DEDUP-EVIDENCE.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`

## Forbidden Paths

- Screening
- persistence/API/UI/cloud
- live providers/network
- import parser behavior changes
- PHP-generated fixture editing
- PHP compatibility claims
- nexus-cli / nexus-web behavior claims

## Implemented Behavior

- Deduplication input is raw Search trace/import sighting and import metadata evidence.
- Exact duplicate clustering is namespace-sensitive and deterministic.
- Exact identifier evidence auto-forms clusters with transitive closure.
- Fuzzy title candidate matching is review-only:
  - local default threshold `0.95` (`95`)
  - `FuzzyTitle` evidence only, no automatic cluster by title.
- No-id candidates remain unresolved and can only create review candidates.
- Stable representative election with deterministic tie-breakers.
- Cluster members preserve source trace/sighting links and evidence edges.
- Cluster representatives are projections; raw member evidence is retained.
- Web projection fields (membership hash, persisted dedup runs, representative snapshots, stale checks) are not domain authority.

## Test Coverage

- `tests/NexusScholar.Core.Tests/DeduplicationServiceTests.cs`
  - exact overlap auto-cluster
  - cross-namespace same-value isolation
  - transitive exact-ID closure
  - fuzzy review-required boundary
  - no-id no auto-merge
  - local threshold boundary behavior
  - deterministic representative election
  - raw evidence and unresolved candidate preservation
  - source/import trace binding and non-claims
  - source trace/sighting binding evidence
- `tests/NexusScholar.Conformance.Tests/DeduplicationFixtureTests.cs`
  - required fixture presence
  - metadata check for local-gate-9-dedup fixture set

## Fixture IDs (local)

- `dedup-exact-doi-cluster`
- `dedup-exact-cross-provider-id-cluster`
- `dedup-transitive-cluster`
- `dedup-fuzzy-title-review-required`
- `dedup-threshold-95-boundary`
- `dedup-no-id-title-only-no-auto-merge`
- `dedup-representative-election`
- `dedup-representative-merge-preserves-evidence`
- `dedup-raw-sightings-preserved`
- `dedup-web-app-projection-not-authority`

## Conflict Status

- `CF-011`: implemented for local Dedup input shape.
- `CF-012`: implemented for local fuzzy threshold (`95` / `0.95`).
- `CF-016`: implemented for Search; narrowed for Dedup handoff by local ADR contract.
- `CF-020`: narrowed and unchanged for app projection boundary.

## Explicit Claims Not Made

- no PHP compatibility
- no PHP-generated fixtures
- no Screening behavior
- no persistence/API/UI/cloud behavior
- no import-parser or live provider implementation in this gate
- no app projection treated as Core authority
