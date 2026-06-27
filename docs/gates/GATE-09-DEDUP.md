# Gate 9 Deduplication Reconnaissance

Status: reconnaissance and planning only. C# Deduplication implementation is not ready.

## Goal

Map pinned PHP Deduplication behavior and prepare fixture/comparator planning before any C# Deduplication implementation.

This document extends Gate 9 porting work after Shared Identity, Search trace, and Search import local slices. It does not alter those accepted scopes.

## Sources Read

- `AGENTS.md`
- `PLANS.md`
- `docs/adr/0001-source-of-truth-and-porting.md`
- `docs/adr/0002-canonical-json-and-digests.md`
- `docs/adr/0007-shared-scientific-identity.md`
- `docs/adr/0010-search-trace-and-plan-contract.md`
- `docs/adr/0011-search-import-source-contract.md`
- `docs/gates/GATE-09-SEARCH.md`
- `docs/gates/GATE-09-SEARCH-IMPORT.md`
- `docs/port/php-search-behavior.md`
- `docs/port/php-search-fixture-plan.md`
- `docs/recon/apps/**`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`
- `specs/SOURCE.lock.json`
- pinned PHP Deduplication module under `../core`
- PHP Deduplication tests
- PHP corpus/snapshot/lock tests affecting Deduplication
- `nexus-cli` Search/Dedup related behavior
- `nexus-web` Deduplication and representative lock behavior

## Branch Scope

Allowed paths:

- `docs/port/php-deduplication-behavior.md`
- `docs/port/php-deduplication-fixture-plan.md`
- `docs/gates/GATE-09-DEDUP.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`

Forbidden paths:

- `src/**`
- `tests/**`
- `fixtures/**`
- `specs/**`
- PHP reference repo changes
- `nexus-cli` changes
- `nexus-web` changes
- generated PHP fixtures
- C# Deduplication implementation

## Behavior Summary

Pinned PHP Deduplication:

- receives a `CorpusSlice`
- uses ordered duplicate policies
- exact policies cover DOI, arXiv, OpenAlex, Semantic Scholar, and PubMed
- title fuzzy policy exists with a threshold conflict
- fingerprint policy exists but is not in the documented default Laravel binding
- uses union-find for transitive clustering
- preserves direct duplicate pair evidence
- elects a representative by completeness plus provider priority
- merges duplicate member fields into the representative when exporting representative corpus output
- can fall back to runtime object identity for no-primary-id PHP objects
- can enforce project lock state before deduplication

PHP app behavior adds important projections:

- CLI Search writes a global deduplicated `all_*.json` through `CorpusSlice` merge behavior.
- Web rebuilds draft corpus with `fromWorksUnsafe()` so Deduplication can inspect every draft member.
- Web persists dedup runs, membership hashes, clusters, cluster members, policy stats, and representative snapshots.
- Web blocks corpus lock until dedup evidence is fresh and complete.
- Web Screening requires a representative-aware locked snapshot.

## Open Conflicts

`CF-011`: blocking for C# Dedup implementation.

Raw Dedup input shape is not resolved. PHP uses `CorpusSlice`, but normal `CorpusSlice` may premerge exact identifier duplicates. Web uses `fromWorksUnsafe()` and C# Search preserves raw traces. C# must define a raw candidate input contract from Search traces/imported sightings before implementation.

`CF-012`: blocking for C# Dedup implementation.

Title fuzzy threshold is unresolved. PHP `TitleFuzzyPolicy` defaults to `92`, Laravel binding uses `95`, docs say `95`, Web persisted runs write `0.95`, and demo seed data includes `0.92`.

`CF-016`: implemented for local Search scope; Dedup handoff pending.

Search already emits raw traces and does not call Deduplication. Deduplication must now define how it consumes those traces without reintroducing Search-time dedupe.

`CF-018`: narrowed for Search consumer boundary; Dedup app boundary pending.

CLI/Web projections are useful evidence but not Core authority. Dedup needs an explicit decision about which app concepts remain projections.

`CF-020`: newly opened for Deduplication app projection and representative snapshot boundary.

Nexus Web has membership hashes, persisted dedup runs, representative scoring fallback, stale run detection, and representative-aware corpus lock snapshots. C# Core must decide which pieces are domain contract, which are snapshot/persistence gate work, and which remain app projections.

## Fixture Plan

Detailed fixture planning lives in `docs/port/php-deduplication-fixture-plan.md`.

Required planned fixture families:

- input shape from Search traces and imported sightings
- exact identifier clustering
- title fuzzy threshold and year-gap behavior
- transitive cluster assembly
- representative election and merge behavior
- no-id unresolved candidates
- raw duplicate evidence preservation
- locked corpus rejection if admitted by local contract
- Web/app projection catalog

Key planned fixture IDs:

- `dedup-input-search-trace-to-candidates.json`
- `dedup-input-imported-sightings-to-candidates.json`
- `dedup-exact-doi-cluster.json`
- `dedup-exact-openalex-cluster.json`
- `dedup-exact-s2-cluster.json`
- `dedup-exact-arxiv-cluster.json`
- `dedup-exact-pubmed-cluster.json`
- `dedup-source-specific-id-not-workid.json`
- `dedup-title-fuzzy-threshold-decision.json`
- `dedup-title-fuzzy-threshold-conflict-92-vs-95.json`
- `dedup-transitive-cluster.json`
- `dedup-transitive-evidence-preserved.json`
- `dedup-representative-election-completeness.json`
- `dedup-representative-election-provider-priority.json`
- `dedup-merge-field-preservation.json`
- `dedup-no-id-candidate-not-auto-merged.json`
- `dedup-raw-duplicate-evidence-preserved.json`
- `dedup-locked-corpus-rejected.json`
- `dedup-app-membership-hash-projection.json`
- `dedup-representative-snapshot-app-projection.json`

## Comparator Plan

Comparators must be built before PHP compatibility claims.

Comparator groups:

- input comparator: Search/import sightings to Dedup candidates
- identifier comparator: normalized namespace/value overlap
- title fuzzy comparator: normalization, threshold, year gap, confidence rounding
- cluster comparator: unordered member sets plus ordered/direct evidence edges
- representative comparator: deterministic election and tie-breakers
- merge comparator: representative field preservation and fill behavior
- app projection comparator: membership hash, persisted run summaries, representative snapshot membership, stale run detection

Generated ids, runtime durations, PHP object ids, and wall-clock retrieval times must not be semantic comparator anchors unless fixture generators pin them.

## Implementation Readiness

Implementation readiness: **no**.

Required before C# Dedup implementation:

- ADR/contract for raw Dedup input shape from Search traces and imported sightings
- ADR/contract decision for title fuzzy threshold and algorithm
- decision on no-id candidate processing without runtime object identity
- decision on representative election deterministic tie-breakers
- decision on app projection boundary for Web membership hash, representative snapshot, persisted runs, and scoring fallback
- local fixture and comparator catalog accepted

## Explicit Claims Not Made

- no C# Deduplication implementation
- no generated PHP fixtures
- no PHP compatibility
- no live provider/network behavior
- no Search implementation change
- no Search import implementation change
- no Screening behavior
- no corpus snapshot implementation
- no Deduplication persistence schema
- no API/UI/cloud behavior
- no `nexus-cli` or `nexus-web` behavior made authoritative
- no blueprint conformance
