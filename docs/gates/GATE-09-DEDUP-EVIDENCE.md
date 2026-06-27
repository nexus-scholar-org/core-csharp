# Gate 9 Dedup Evidence

Status: local implementation and review-blocker fixes applied; gate evidence is replay-based and remains pending hosted CI, review, and merge approval.

## Scope Implemented Locally

- deterministic exact identifier clustering using `ADR 0007` identity
- namespace-sensitive matches only
- fuzzy title review candidates at local threshold `95` / `0.95`
- transitive exact-ID clustering
- deterministic representative election and projection behavior
- no-id unresolved candidates and review-only path
- raw search/import evidence retention in cluster output
- imported-export source-file digest, digest scope, raw-record digest, parser warning, and record notice retention in Dedup raw candidates and source evidence
- imported-export source-file digest, digest scope, raw-record digest, parser warning, and record notice projection onto representatives when imported records are clustered
- app projection boundary non-claims (membership hash/run/snapshot fields are non-authoritative)
- fixture replay now asserts evidence classes, unresolved/review counts, cluster/member behavior, threshold pass-through, canonical fixture digest metadata, parser warning preservation, and source-file/raw-record digest propagation where present.

## Branch and Command Surface

- Branch: `cdx/gate-9-dedup-local`
- Result schema: `nexus.deduplication.result` / `1.0.0`
- Default fuzzy threshold: `0.95`

## Evidence Artifacts

- local source and conformance fixtures under `fixtures/conformance/deduplication/`
- hand-authored fixture `inputDigest` values are recomputed from canonical `case` content.
- hand-authored fixture `outputDigest` values are recomputed from canonical expected replay summaries.

## Required Fixture IDs

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
- `dedup-fuzzy-title-below-threshold-no-review`
- `dedup-source-specific-id-not-workid-review-only`

## Verification Commands

```text
dotnet restore NexusScholar.Core.slnx
dotnet build NexusScholar.Core.slnx -c Release --no-restore

dotnet test NexusScholar.Core.slnx -c Release --no-build
dotnet format NexusScholar.Core.slnx --verify-no-changes --no-restore
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

## Latest Local Verification

- Date: 2026-06-27
- Command: `powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1`
- Result: passed
- Architecture tests: 14 passed
- Core tests: 160 passed
- Conformance tests: 55 passed

## Conflict Summary

- `CF-011`: resolved for local Dedup input.
- `CF-012`: resolved for local fuzzy threshold default.
- `CF-020`: narrowed for app projection behavior.
- `CF-016`: implemented for Search and consumed by Dedup local handoff.

## Explicit Non-Claims

- no PHP compatibility claim
- no PHP-generated fixture claim
- no Screening
- no persistence/API/UI/cloud behavior
- no live provider/network behavior
- no App projection behavior treated as Core authority
