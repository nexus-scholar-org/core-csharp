# Gate 9 Search Evidence

Status: local and hosted verification recorded for `cdx/gate-9-search-local`.

## Hosted CI

Implementation commit:

```text
7ea3e99d2a2ea9cc9beef40ee46c97c91cd915e3
```

Hosted CI run:

```text
https://github.com/nexus-scholar/core-csharp/actions/runs/28290056865
```

Hosted matrix:

```text
verify (ubuntu-latest): success
verify (windows-latest): success
```

Steps passed on both:

```text
checkout
setup .NET
restore
build
test
format
```

## Scope Accepted Locally

Gate 9 Search local implementation covers deterministic stub-provider Search traces only:

- raw Search trace output with schema id `nexus.search.trace` and schema version `1.0.0`
- validated Search request shape
- provider alias normalization, empty-alias removal, deduplication, and unknown-provider rejection before execution
- provider registration-order execution for selected stub providers
- provider-order-insensitive cache identity
- `include_raw_data` included in local C# cache identity
- provider attempts, provider stats, partial failures, and all-failed traces
- duplicate provider sightings preserved
- no-id records preserved as unresolved candidates
- schema-closed local Search plan parsing
- explicit legacy PHP plan import/comparator profile
- local fixtures under `fixtures/conformance/search/`

## Fixture IDs

- `search-query-validation.json`
- `search-cache-key-provider-order.json`
- `search-cache-key-field-inclusion.json`
- `search-cache-key-field-exclusion.json`
- `search-cache-key-active-provider-set.json`
- `search-cache-key-include-raw-data-included.json`
- `search-provider-selection-all.json`
- `search-provider-selection-subset.json`
- `search-provider-selection-unknown-alias.json`
- `search-provider-partial-failure.json`
- `search-provider-all-failed-empty.json`
- `search-trace-schema-closed-plan.json`
- `search-trace-php-legacy-plan-import.json`
- `search-trace-raw-provider-results.json`
- `search-trace-duplicate-provider-sightings.json`
- `search-trace-no-id-candidates.json`
- `search-trace-raw-data-preserved.json`
- `search-trace-raw-data-not-requested.json`
- `search-trace-dedup-not-applied.json`

## Local Verification

Commands run:

```text
dotnet restore NexusScholar.Core.slnx
dotnet build NexusScholar.Core.slnx -c Release
dotnet build NexusScholar.Core.slnx -c Release --no-restore
dotnet test NexusScholar.Core.slnx -c Release --no-build
dotnet format NexusScholar.Core.slnx --verify-no-changes --no-restore
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

Results:

```text
NexusScholar.Architecture.Tests: 13 passed
NexusScholar.Conformance.Tests: 51 passed
NexusScholar.Core.Tests: 130 passed
Total: 194 passed, 0 failed
```

After evidence updates, rerun the hosted matrix on the final branch head before merge.

```text
hosted Windows/Linux matrix after final evidence push
```

## Conflict Status

- `CF-013`: implemented for local Search cache identity.
- `CF-016`: implemented for local raw Search trace and no-Deduplication boundary.
- `CF-017`: implemented for local schema-closed Search plans.
- `CF-018`: unchanged; narrowed for Search consumer boundary, broader app alignment pending.
- `CF-019`: unchanged; future imported-export Search source contract remains pending and does not block local stub-provider Search.

## Explicit Non-Claims

- no PHP compatibility
- no generated PHP fixtures
- no live provider/network behavior
- no import parser implementation
- no Scopus API
- no Web of Science API
- no Google Scholar scraping
- no Deduplication
- no Screening
- no persistence/API/UI/cloud
- no app behavior made authoritative
