# Gate 9 Full Text

Status: local no-network C# implementation complete for the first `ADR 0014` slice. Live provider, network, PDF extraction, OCR, persistence, API, UI, cloud, and PHP compatibility behavior remain out of scope.

## Goal

Implement the first local Full Text evidence bridge between Screening/candidate-set handoff and digest-bound artifact evidence.

The implemented slice covers:

- `nexus.fulltext.input` records from final title/abstract `include` and allowed `needs_review` Screening handoff;
- rejection of raw Search trace input and raw Dedup member input;
- default rejection of final title/abstract `exclude` decisions as retrieval candidates;
- `nexus.fulltext.acquisition-record` records with actor/timestamp requirements for user-supplied and manual acquisitions;
- ordered source attempts preserving failure, skipped, manual-needed, and success states;
- `nexus.fulltext.artifact-evidence` records for `pdf`, `xml`, `text`, and `derived-text`;
- raw byte digest validation using `raw-artifact-bytes`;
- local validators for PDF signature, XML/HTML shape, text emptiness, media type, and max size;
- duplicate artifact detection by raw byte digest only;
- `nexus.fulltext.extraction-record` records binding derived evidence to source artifact id and source raw digest;
- app/PHP/CLI/Web projection rejection as Core authority.

## Sources Read

- `AGENTS.md`
- `PLANS.md`
- `docs/adr/0001-source-of-truth-and-porting.md`
- `docs/adr/0002-canonical-json-and-digests.md`
- `docs/adr/0007-shared-scientific-identity.md`
- `docs/adr/0008-provenance-ledger.md`
- `docs/adr/0009-portable-bundle-and-artifact-contract.md`
- `docs/adr/0010-search-trace-and-plan-contract.md`
- `docs/adr/0011-search-import-source-contract.md`
- `docs/adr/0012-deduplication-evidence-and-cluster-contract.md`
- `docs/adr/0013-screening-decision-and-conflict-contract.md`
- `docs/adr/0014-fulltext-acquisition-artifact-and-extraction-contract.md`
- `docs/port/php-fulltext-behavior.md`
- `docs/port/php-fulltext-fixture-plan.md`
- `docs/port/OPEN-CONFLICTS.md`
- `docs/port/GOLDEN-FIXTURE-PLAN.md`

## Implemented Files

- `src/NexusScholar.FullText/`
- `tests/NexusScholar.Core.Tests/FullTextTests.cs`
- `tests/NexusScholar.Conformance.Tests/FullTextFixtureTests.cs`
- `fixtures/conformance/fulltext/*.json`
- architecture and project wiring for the new domain assembly

## Fixture Status

The local fixture set under `fixtures/conformance/fulltext/` is hand-authored local conformance evidence only. It does not claim generated PHP fixture status or PHP compatibility.

Implemented fixture ids:

- `fulltext-input-from-screening-include.json`
- `fulltext-input-from-screening-needs-review.json`
- `fulltext-reject-raw-search-trace.json`
- `fulltext-reject-raw-dedup-member.json`
- `fulltext-exclude-not-retrievable-by-default.json`
- `fulltext-user-supplied-pdf-artifact.json`
- `fulltext-user-supplied-xml-artifact.json`
- `fulltext-user-supplied-text-artifact.json`
- `fulltext-deterministic-stub-artifact.json`
- `fulltext-local-path-not-identity.json`
- `fulltext-missing-raw-digest.json`
- `fulltext-wrong-digest-scope.json`
- `fulltext-digest-mismatch.json`
- `fulltext-invalid-pdf-signature.json`
- `fulltext-html-not-fulltext-xml.json`
- `fulltext-empty-text-artifact.json`
- `fulltext-artifact-too-large.json`
- `fulltext-source-failure-followed-by-success.json`
- `fulltext-duplicate-artifact-digest.json`
- `fulltext-derived-extraction-binds-source-artifact.json`
- `fulltext-partial-extraction-warning.json`
- `fulltext-app-projection-not-authority.json`

## Verification

See `docs/gates/GATE-09-FULLTEXT-EVIDENCE.md` for command output summary and final verification status.

## Conflict Status

`CF-025`: implemented/resolved for local Full Text artifact evidence.

Full Text artifact evidence uses exact accepted bytes plus `raw-artifact-bytes` digest. PHP storage paths, CLI manifest paths, Web routes, app row ids, and local paths remain projections.

`CF-026`: remains narrowed.

The implemented C# slice is no-network and supports user-supplied bytes, deterministic stub artifacts, manual acquisition records, source-reference metadata, validation, and digest-bound evidence. Live providers, HTTP downloads, provider SDKs, credentials, scraping, paywall bypass, and shadow-library sources remain blocked.

`CF-027`: remains narrowed.

Core Full Text records preserve the app projection boundary. PHP `pdf_fetches`, CLI manifests, Web batches/items, app audit rows, routes, storage paths, and app row ids are not Core authority.

`CF-024`: unchanged.

Screening app workflow rows remain projections.

## Explicit Non-Claims

- no live provider/network behavior
- no HTTP clients
- no Unpaywall, PMC, Europe PMC, arXiv, OpenAlex, Semantic Scholar, publisher, or Direct PDF integration
- no provider SDKs or credentials
- no paywall bypass
- no shadow-library source
- no Google Scholar scraping
- no actual PDF parsing implementation
- no OCR implementation
- no persistence/API/UI/cloud behavior
- no CLI/Web behavior changes
- no PHP reference repo changes
- no generated PHP fixtures
- no PHP compatibility claim
- no Search/Deduplication/Screening behavior changes
- no artifact storage implementation
- no bundle behavior change
- no blueprint conformance
