# ADR 0011: Search Import Source Contract

Status: Accepted

Date: 2026-06-27

## Context

`ADR 0010` defines Search output as a raw `nexus.search.trace` and admits three Search acquisition kinds: `stub-provider`, `live-provider`, and `imported-export`. Gate 9 local Search is implemented only for deterministic stub-provider traces. Live provider/network behavior remains out of scope.

`CF-019` tracks the unresolved imported Search source contract. Imported Search sources are user-supplied external search exports: the user performs a search outside Nexus, exports evidence from a database or tool, and Nexus later imports that export as Search evidence. This differs from live provider behavior, where Nexus performs the query.

This ADR defines the imported-export source contract needed before local import parser implementation. It does not implement parsers, generate fixtures, call providers, authorize APIs, scrape Google Scholar, change CLI/Web behavior, or claim PHP compatibility.

## Decision

### 1. Imported Search Source identity

An imported Search source record must identify the external export and parser projection. The local acquisition kind is:

```text
imported-export
```

The import source identity must carry:

- `acquisition_kind = imported-export`
- `source_database_or_tool`
- `export_format`
- `parser_id`
- `parser_version`
- `source_file_digest`
- `imported_at`
- `imported_by`, when available
- `original_query_text`, when available
- `exported_at`, when available
- `record_count`
- `parser_warnings`

The record may also carry source-provided export metadata such as source collection name, export option labels, or source-reported result count when available. Those fields are evidence metadata, not scientific work identity.

`source_file_digest`, `parser_id`, and `parser_version` are required for any accepted parser output. Parser output without a bound source file digest is not canonical Search import evidence.

### 2. Supported future import families

The import contract admits these future user-supplied export families:

- RIS
- BibTeX
- Scopus CSV or export files
- Web of Science export files
- Zotero/CSL JSON
- EndNote export files
- Publish or Perish CSV or other Google Scholar-derived user exports

Admitting these families at contract level does not implement parser support for them. Each parser still requires implementation, fixtures, and comparator tests.

### 3. Raw evidence rule

Raw exported file bytes must be preserved or digest-bound.

`source_file_digest` must use the accepted raw-byte digest rule. When the export is represented as a Nexus artifact, the digest scope should be `raw-artifact-bytes` from `ADR 0002` and `ADR 0009`.

The digest input is the exact exported file bytes. Line endings, encodings, field ordering, file names, local paths, and parser-normalized output are not substituted for the source bytes.

Parser output is a normalized projection over raw import evidence. It is not the raw evidence itself.

Local filesystem paths, original upload paths, temporary paths, drive letters, and file names must not become Search identity or scientific work identity. They may appear only as operator-facing diagnostics when needed and must not enter canonical import identity.

### 4. Search trace integration

Imported records become Search trace sightings or unresolved Search candidates.

Imported records must preserve source context, including:

- `source_database_or_tool`
- `export_format`
- source record id when present
- source-specific identifier evidence when present
- parser warning/error evidence when relevant

Imported records do not become deduplicated corpus membership. Search still does not call Deduplication. Duplicate imported records and imported records that overlap with existing provider sightings remain separate Search trace evidence until a later Deduplication stage consumes the trace.

No-id imported records remain unresolved Search candidates. They must not use title-only identity, runtime object identity, source row order, local file path, source file name, app display hash, or parser-generated row id as scientific identity.

### 5. Source-specific identifiers

This ADR does not expand the `ADR 0007` `WorkIdNamespace` set.

Identifiers in the existing namespace set may normalize into `WorkId` values when present:

- DOI
- arXiv
- PubMed / PMID
- PMCID
- OpenAlex
- Semantic Scholar / S2
- IEEE
- DOAJ
- internal

Scopus EID, Web of Science UT/accession numbers, source row ids, EndNote record numbers, Zotero keys, RIS accession-like fields, and other source-specific ids remain `source_record_id` or `source_identifier` evidence until a later ADR explicitly extends WorkId namespaces.

A parser must not silently promote a source-specific id into `WorkIdNamespace.Internal` just to make a record identified. Internal ids are reserved for accepted local internal identifiers, not arbitrary source export ids.

### 6. Google Scholar boundary

Google Scholar scraping is not authorized.

Imported Google Scholar-derived evidence may be accepted only as user-supplied export evidence, such as Publish or Perish CSV or another user-provided export file. Nexus must not automate Google Scholar scraping, crawling, request bypass behavior, captcha avoidance, browser automation, or unofficial Google Scholar data extraction under this ADR.

Future Google Scholar-derived import support must treat the export file as user-supplied evidence and must bind the raw exported bytes through `source_file_digest`.

### 7. Scopus and Web of Science boundary

Scopus API integration is not authorized.

Web of Science API integration is not authorized.

User-supplied Scopus and Web of Science exports are allowed as imported evidence under this ADR. Nexus may later parse those user-supplied files, but it must not query Scopus or Web of Science APIs, handle API credentials, use provider SDKs, or perform live provider/network behavior without a later provider/network gate.

### 8. Parser warning and error model

Import parsers must report stable warning and error categories.

Required categories include:

- `unsupported-import-format`
- `missing-source-file-digest`
- `missing-required-field`
- `malformed-record`
- `unknown-identifier-type`
- `duplicate-source-record-id`
- `parser-warning-preserved`
- `skipped-record`

A malformed or skipped record should be preserved as warning or error evidence where possible. If a parser can identify the source record location or source record id, that context should be included in the warning/error record.

Parser warnings are part of import audit evidence. They are not Deduplication decisions, Screening decisions, provider failures, or proof of source database correctness.

### 9. Fixture consequences

Future local import fixtures must include:

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

Negative cases must cover:

- unsupported import format;
- missing source file digest;
- missing required field;
- malformed record;
- unknown identifier type;
- duplicate source record id;
- parser warning preservation;
- skipped record evidence;
- source-specific id not promoted to `WorkIdNamespace`;
- imported title-only duplicate not deduped by Search;
- Google Scholar scraping rejected.

Fixtures are local conformance fixtures until a later PHP compatibility fixture generator and comparator exists. Hand-authored local fixtures do not create PHP compatibility claims.

### 10. Implementation readiness

Future local import parser implementation is ready to plan against this ADR, with these limits:

- yes for local parsers over user-supplied exports;
- yes for local conformance fixtures that exercise this contract;
- no for live provider/API implementation;
- no for Scopus API integration;
- no for Web of Science API integration;
- no for Google Scholar scraping;
- no for PHP compatibility;
- no for generated PHP fixtures;
- no for Deduplication or Screening behavior.

## Alternatives Considered

### Treat imported exports as live providers

Rejected.

Imported exports are user-supplied evidence files. Live providers require network, credentials, retries, rate limits, provider terms, and external API behavior, which are outside this gate.

### Promote source-specific ids into WorkId namespaces now

Rejected.

Scopus EID, Web of Science UT/accession numbers, Zotero keys, and similar source identifiers may be important evidence, but adding scientific identity namespaces affects Shared Identity, Deduplication, snapshots, and compatibility fixtures. That requires a later ADR.

### Allow title-only import identity

Rejected.

`ADR 0007` rejects title-only scientific identity. Imported records without stable identifiers remain unresolved candidates.

### Accept Google Scholar scraping as an import mechanism

Rejected.

This ADR covers user-supplied export evidence only. Scraping, crawling, browser automation, and bypass behavior are provider/network activities and are not authorized.

## Consequences

Positive:

- `CF-019` is resolved for imported Search source contract planning.
- Future import parsers have a bounded evidence shape before implementation.
- User-supplied Scopus, Web of Science, Zotero, EndNote, RIS, BibTeX, and Publish or Perish exports can be planned without authorizing live provider APIs.
- Source-specific ids remain available as evidence without weakening `ADR 0007`.
- Search continues to preserve raw trace evidence and avoids Search-time Deduplication.

Negative:

- No import parser behavior is available until implementation.
- Source-specific identifiers cannot yet participate in WorkId overlap equality.
- PHP compatibility remains unclaimed.
- Live provider/API work remains blocked by a future provider/network gate.

## Migration Effect

No persisted C# data is migrated by this ADR.

Any future imported-export records created before parser implementation must be treated as non-authoritative unless they are replayed or validated under this ADR.

Any app or external import output that lacks `source_file_digest`, parser id/version, parser warnings, and source context must be staged as non-canonical import evidence until transformed under this contract.

## Fixture Effect

`docs/port/GOLDEN-FIXTURE-PLAN.md` and `docs/port/php-search-fixture-plan.md` must list imported-export fixture families and negative cases under this ADR.

Fixture metadata must record source kind, source refs, source commit or local source note, generator command, generator version, input digest, output digest, and semantic comparison rules.

No fixture generated or hand-authored for this ADR may call live providers, query Scopus/Web of Science APIs, scrape Google Scholar, or imply PHP compatibility.

## Conflict Effect

`CF-019` is resolved for the local imported Search source contract by this ADR. Parser implementation, concrete supported format parsers, PHP compatibility, source-specific namespace expansion, provider/API integrations, and app alignment remain future work.

`CF-013`, `CF-016`, `CF-017`, and `CF-018` are unchanged.

## Reversal Conditions

Revise this ADR only if:

1. a later provider/network ADR admits live Scopus, Web of Science, Google Scholar-derived, or other provider acquisition behavior and needs a different acquisition record;
2. a later Shared Identity ADR extends WorkId namespaces for source-specific ids;
3. generated fixture evidence shows the parser warning/error model cannot represent required import evidence;
4. app alignment promotes specific imported-export app fields into Core records with digest and migration rules;
5. a later bundle or artifact ADR changes the raw-byte digest binding for imported source files.

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
