# Gate 9 Search Trace And Plan

Status: local stub-provider Search implementation scope. PHP compatibility, live providers, import parsers, Deduplication, Screening, persistence, API/UI/cloud, and app authority remain unclaimed.

## Goal

Implement the local C# Search trace and plan behavior defined by `ADR 0010`, using deterministic stub providers only.

This gate document builds on PHP Search reconnaissance and `ADR 0010`. It covers the Search portion of Gate 9 porting work and does not change the accepted Gate 9 shared identity implementation.

## Sources Read

- `AGENTS.md`
- `PLANS.md`
- `docs/adr/0001-source-of-truth-and-porting.md`
- `docs/adr/0002-canonical-json-and-digests.md`
- `docs/adr/0007-shared-scientific-identity.md`
- `docs/adr/0009-portable-bundle-and-artifact-contract.md`
- `docs/adr/0010-search-trace-and-plan-contract.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`
- `docs/recon/apps/**`
- `specs/SOURCE.lock.json`
- pinned PHP Search module under `../core`
- PHP Search unit and integration tests
- PHP search plan fixtures
- PHP VCR cassette catalog

## Branch Scope

Allowed paths:

- `NexusScholar.Core.slnx`
- `src/NexusScholar.Search/**`
- `src/NexusScholar.Kernel/**` only if a primitive is genuinely reusable
- `src/NexusScholar.Shared/**` only if using existing shared identity primitives without changing identity semantics
- `tests/NexusScholar.Core.Tests/**`
- `tests/NexusScholar.Architecture.Tests/**`
- `tests/NexusScholar.Conformance.Tests/**`
- `fixtures/conformance/search/**`
- `docs/gates/GATE-09-SEARCH.md`
- `docs/gates/GATE-09-SEARCH-EVIDENCE.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`

Forbidden paths:

- live provider/network adapters
- HTTP clients
- API keys or credentials
- Scopus API
- Web of Science API
- Google Scholar scraping
- imported-export parsers
- PHP-generated fixtures
- Deduplication
- Screening
- persistence/API/UI/cloud
- `nexus-cli`
- `nexus-web`

## Reconnaissance Summary

Pinned PHP Search behavior includes:

- validated `SearchQuery`, `SearchTerm`, and `YearRange`
- provider aliases: `openalex`, `crossref`, `semantic_scholar`, `arxiv`, `pubmed`, `ieee`, and `doaj`
- provider-order-insensitive cache identity
- selected-provider validation before cache lookup and execution
- partial provider failure reporting through provider stats
- provider-specific pagination, retry, rate-limit, and normalization behavior
- YAML plan parsing for current `searches` and legacy `queries`
- optional raw provider data preservation
- persistent search-run recording in Laravel-facing paths
- PHP Search-time deduplication before return/cache

The PHP Search-time deduplication boundary is not safe to port directly into C# Search because Gate 9 shared identity explicitly did not resolve Search, Deduplication, Screening, or corpus snapshot behavior.

## Implemented Local Behavior

The local C# Search implementation:

- emits a raw `nexus.search.trace` version `1.0.0`, not a deduplicated corpus
- validates query length, year ranges, max results, offset, and provider aliases
- normalizes provider aliases by trim/lowercase, drops empty aliases, and deduplicates aliases
- treats empty selected provider aliases as all active local stub providers
- executes selected providers in registration order
- rejects unknown provider aliases before provider execution and cache identity use
- computes provider-order-insensitive cache identity over query, year range, language, max results, offset, sorted active provider aliases, and `include_raw_data`
- excludes query id, trace id, project id, runtime data, provider stats/failures, raw bytes, app ids/hashes, local paths, and credentials from cache identity
- records provider attempts, provider stats, partial provider failure, and all-failed valid traces
- preserves duplicate provider sightings
- preserves no-id records as unresolved candidates, not canonical corpus membership
- strips raw payloads unless `include_raw_data` is requested
- parses authoritative local Search plans as schema-closed artifacts
- admits PHP-permissive plans only through an explicit legacy import/comparator parser
- uses ADR 0007 shared identity primitives for normalized identifiers without title-only identity
- does not call Deduplication

## Conflict Status

`CF-013`: implemented for local Search cache identity. C# cache identity remains provider-order-insensitive, includes term, year range, language, max results, offset, sorted active provider aliases, and `include_raw_data`, and excludes generated query id, trace id, project id, runtime data, provider stats/failures, raw bytes, app ids/hashes, local paths, and provider credentials. PHP compatibility remains pending because PHP excludes `includeRawData`.

`CF-016`: implemented for local Search raw trace behavior. C# Search output is a raw Search trace, preserves duplicate provider sightings, and does not call Deduplication. Deduplication remains a later gate and will consume Search traces as input.

`CF-017`: implemented for local Search plan schema closure. Authoritative local C# Search plan artifacts are schema-closed. PHP-permissive plan parsing is allowed only as an explicit legacy import/comparator profile.

`CF-018`: narrowed for the Search consumer boundary by `ADR 0010`. CLI/Web may consume Search traces and display projections, but app display hashes, run files, database rows, job lifecycle rows, audit rows, latest pointers, and app manifests are not Core authority.

`CF-019`: opened as future planning by `ADR 0010`. Imported external Search exports are admitted as future acquisition evidence, but import parser behavior, supported formats, source-specific identifier handling, and parser comparator policy remain future work. This does not block local stub-provider Search implementation.

## Fixture Plan

Implemented local fixture families are recorded in `docs/port/php-search-fixture-plan.md`, `docs/port/GOLDEN-FIXTURE-PLAN.md`, and `fixtures/conformance/search/`.

- query and cache identity fixtures
- provider selection and execution fixtures
- search plan parsing fixtures
- raw Search trace and Deduplication-boundary fixtures
- imported-export fixture families, deferred until a Search import contract
- locked-project and persistence-shape fixtures only if admitted by a later scope

Implemented local fixture IDs:

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

Deferred fixture IDs:

- `search-plan-parse-nexus-cli-v4.json`
- `search-plan-parse-legacy-queries.json`
- `search-plan-item-overrides.json`
- `search-normalize-openalex-stub.json`
- `search-normalize-semantic-scholar-stub.json`
- `search-normalize-crossref-stub.json`
- `search-normalize-arxiv-stub.json`
- `search-normalize-pubmed-stub.json`
- `search-normalize-ieee-stub.json`
- `search-normalize-doaj-stub.json`
- `search-import-ris-trace.json`
- `search-import-bibtex-trace.json`
- `search-import-scopus-csv-trace.json`
- `search-import-wos-export-trace.json`
- `search-import-zotero-csl-json-trace.json`
- `search-import-endnote-export-trace.json`
- `search-import-publish-or-perish-csv-trace.json`
- `search-import-source-file-digest.json`
- `search-import-parser-warning.json`
- `search-import-no-id-candidates.json`
- `search-import-dedup-not-applied.json`

## Negative Cases

Required negative cases:

- invalid search term
- invalid year range
- unknown provider alias
- non-positive max results or plan limit
- invalid YAML
- non-list `searches` or `queries`
- non-mapping plan item
- missing item id
- missing query/text
- non-mapping metadata
- missing selected plan id
- partial provider failure
- all providers failed
- duplicate provider sightings must not be deduped by Search
- title-only overlap must not become Search identity
- no-id candidate must not become canonical membership identity
- raw-data request must not be satisfied by a non-raw cache entry under the local C# cache contract
- PHP raw-data cache ambiguity must be classified as an intentional incompatibility unless a later ADR reverses `ADR 0010`
- unsupported import format, if imported-export parsing is admitted later
- missing source file digest, if imported-export parsing is admitted later
- parser warning must be preserved, if imported-export parsing is admitted later
- source-specific id must not be promoted to an ADR 0007 WorkId namespace without a later ADR
- imported title-only duplicate must not be deduped by Search
- Google Scholar scraping must not be allowed

## Comparator Plan

Comparators must be built before compatibility claims.

Comparator groups:

- cache comparator: exact included/excluded fields and hash equality/inequality
- plan parser comparator: normalized item shape, ordering, overrides, filters, and stable errors
- provider selection comparator: active aliases, validation order, stats order, partial failure shape
- provider normalization comparator: identifiers, title, year, authors, venue, raw payload presence/digest
- Search trace comparator: raw provider sightings, duplicates, order, provider stats, and no Deduplication output

Generated ids, runtime durations, and live HTTP timing must not be semantic comparator anchors unless the fixture generator pins them.

## Implementation Readiness

Implemented locally for deterministic stub-provider C# Search only.

Still blocked for:

- live provider/network behavior
- PHP compatibility claims
- generated PHP fixtures
- Search persistence/API/UI/job/cloud behavior
- Deduplication and Screening behavior
- bundle behavior changes
- app behavior authority beyond Search trace consumption
- imported-export parser implementation; a future Search import contract is required

## Explicit Claims Not Made

- no import parser implementation
- no provider/network behavior
- no live provider/network behavior
- no Scopus API integration
- no Web of Science API integration
- no Google Scholar scraping
- no PHP compatibility
- no generated PHP fixtures
- no Deduplication behavior
- no Screening behavior
- no Search persistence schema
- no API, UI, job, command, cloud, or provider SDK behavior
- no bundle behavior change
- no AI governance behavior
- no blueprint conformance
