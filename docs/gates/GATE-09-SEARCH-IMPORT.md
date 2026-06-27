# Gate 9 Search Import Source Contract

Status: ADR/contract only. No import parsers, source code, fixtures, provider/network behavior, PHP compatibility, CLI/Web behavior, or app authority are implemented by this branch.

## Goal

Define the Search imported-export source contract for user-supplied external search exports before any C# import parser implementation.

This gate page applies `ADR 0011` to Gate 9 Search import planning. Gate 9 local Search remains implemented only for deterministic stub-provider traces under `ADR 0010`.

## Sources Read

- `AGENTS.md`
- `PLANS.md`
- `docs/adr/0001-source-of-truth-and-porting.md`
- `docs/adr/0002-canonical-json-and-digests.md`
- `docs/adr/0007-shared-scientific-identity.md`
- `docs/adr/0009-portable-bundle-and-artifact-contract.md`
- `docs/adr/0010-search-trace-and-plan-contract.md`
- `docs/adr/0011-search-import-source-contract.md`
- `docs/gates/GATE-09-SEARCH.md`
- `docs/port/php-search-behavior.md`
- `docs/port/php-search-fixture-plan.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`
- `docs/recon/apps/**`

## Branch Scope

Allowed paths:

- `docs/adr/0011-search-import-source-contract.md`
- `docs/gates/GATE-09-SEARCH-IMPORT.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`
- `docs/port/php-search-fixture-plan.md`

Forbidden paths:

- `src/**`
- `tests/**`
- `fixtures/**`
- `specs/**`
- PHP reference repo changes
- `nexus-cli`
- `nexus-web`
- import parser implementation
- provider/network behavior
- generated fixtures

## Decisions

`ADR 0011` defines:

- `acquisition_kind = imported-export`
- source identity fields for user-supplied exports
- supported future import families: RIS, BibTeX, Scopus export, Web of Science export, Zotero/CSL JSON, EndNote export, and Publish or Perish CSV
- raw exported file byte preservation or digest binding through `source_file_digest`
- local paths excluded from Search identity
- parser output as a projection over raw evidence
- imported records as Search trace sightings or unresolved candidates
- no Search-time Deduplication
- no title-only identity
- no expansion of `ADR 0007` WorkId namespaces
- source-specific ids retained as `source_record_id` or `source_identifier` evidence
- Google Scholar scraping rejected
- Scopus and Web of Science APIs rejected for this gate
- parser warning and error categories

## Conflict Status

`CF-019`: resolved for the local imported Search source contract by `ADR 0011`. Parser implementation, supported format parsers, source-specific identifier namespace expansion, PHP compatibility, provider/API integrations, and app alignment remain future work.

Unchanged:

- `CF-013`: implemented for local Search cache identity.
- `CF-016`: implemented for local raw Search trace and no-Deduplication boundary.
- `CF-017`: implemented for local schema-closed Search plans.
- `CF-018`: narrowed for Search consumer boundary; broader app alignment remains pending.

## Fixture Consequences

Future imported-export fixture IDs:

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
- `search-import-source-specific-id-not-workid.json`
- `search-import-google-scholar-scraping-rejected.json`

Negative cases:

- unsupported import format
- missing source file digest
- missing required field
- malformed record
- unknown identifier type
- duplicate source record id
- parser warning preserved
- skipped record preserved as evidence where possible
- source-specific id not promoted to `WorkIdNamespace`
- imported title-only duplicate not deduped by Search
- Google Scholar scraping rejected

## Implementation Readiness

Ready:

- future local parser implementation over user-supplied export files, after a bounded implementation task is opened
- local conformance fixture generation for imported-export parser behavior

Not ready:

- live provider/API implementation
- Scopus API integration
- Web of Science API integration
- Google Scholar scraping
- PHP compatibility
- generated PHP fixtures
- Deduplication
- Screening
- persistence/API/UI/cloud
- CLI/Web behavior changes

## Explicit Claims Not Made

- no import parser implementation
- no source code changes
- no fixture generation
- no PHP compatibility
- no generated PHP fixtures
- no live provider/network behavior
- no Scopus API integration
- no Web of Science API integration
- no Google Scholar scraping
- no provider SDKs or credentials
- no Deduplication behavior
- no Screening behavior
- no persistence/API/UI/cloud behavior
- no CLI/Web behavior changes
- no app behavior made authoritative
- no bundle behavior change
- no AI governance behavior
- no blueprint conformance
