# Phase 7 Compatibility Evidence Plan

Status: complete.

## Objective

Limit every PHP compatibility statement to reproducible observations from the commit pinned in `specs/SOURCE.lock.json`, then classify C# differences through reviewed semantic comparators. PHP remains behavioral evidence, not authority over accepted specifications or ADRs.

## Evidence Contract

Each generated fixture set must contain:

- immutable replay input;
- PHP-generated expected output;
- a generated manifest with repository, pinned commit, source refs, exact command, generator version, environment assumptions, and SHA-256 input/output digests;
- a reviewed case-by-case classification using `equivalent_serialization`, `intentional_change`, `php_defect`, `csharp_defect`, or `unresolved_specification_conflict`;
- C# conformance tests that validate provenance, digests, classification coverage, equivalent semantics, and exact intentional boundaries.

Generation must fail when the PHP checkout is not at the pinned commit or has tracked modifications. Normal CI replays committed fixtures without calling PHP, live providers, or live LLMs.

## Jobs

| Job | Scope | State | Exit evidence |
| --- | --- | --- | --- |
| H25 | Fixture harness and Shared Identity | complete | deterministic PHP exporter, 12 total cases (`9 equivalent`, `3 intentional_change`) in `php-golden/shared-identity/v1`, manifest digests, reviewed classifications, C# comparator |
| H26 | Search query, cache, provider selection, and local import boundary | complete | 18 total cases (`15 equivalent`, `3 intentional_change`) in `php-golden/search/v1`, semantic comparators, and explicit imported-export non-claim |
| H27 | Deduplication plus corpus lock/snapshot behavior | complete | 16 total cases (`8 equivalent`, `8 intentional_change`) in `php-golden/deduplication/v1`, runtime identity and threshold differences classified, lock/snapshot non-adoption governed by ADR 0026 |
| H28 | Screening and local Full Text overlap | complete | 26 total cases (`16 equivalent`, `9 intentional_change`, `1 php_defect`) in `php-golden/screening-fulltext/v1`, typed comparator test coverage |
| H29 | Citation network and dissemination exports closeout | complete | 14 total cases (all `intentional_change`), generated evidence-only set in `php-golden/citation-export/v1`, no C# replay target, no Network/Reporting implementation, and no broad PHP compatibility claim |

## H25 Evidence

- Generator: `scripts/php-golden/shared-identity-export.php`
- Fixture set: `fixtures/php-golden/shared-identity/v1/`
- PHP source: `nexus-scholar/core@b24d0d71ec7b64003465182477e7edb7f49994f4`
- Equivalent behaviors: normalization, primary precedence, overlap, identifier-set semantics across ordering differences, left-biased title/id merge, direct corpus deduplication, no-id candidate separation, and title lookup.
- Intentional changes under ADR 0007: strict multiple-separator rejection, blank normalized identifier rejection, and no runtime-object-identity deduplication.

## H26 Evidence

- Generator: `scripts/php-golden/search-export.php`
- Fixture set: `fixtures/php-golden/search/v1/`
- Equivalent surface: query/year validation, alias normalization, cache relations, legacy-plan import, provider selection, and normalized partial/all failure behavior.
- Intentional changes under ADR 0010: raw-data-aware local cache identity, schema-closed authoritative plans, and raw Search sightings instead of Search-time deduplication.
- Corrected defects: future `year_from` rejection, below-minimum `year_to` rejection, normalization of exceptions thrown by provider execution, and exclusion of post-provider processing defects from that failure boundary.
- Imported-export behavior remains local ADR 0011 evidence because the pinned PHP Search package has no corresponding import parser surface.

## H27 Evidence

- Generator: `scripts/php-golden/deduplication-export.php`
- Fixture set: `fixtures/php-golden/deduplication/v1/`
- Equivalent surface: five exact namespaces, empty input, exact transitive closure, and fill-only representative merge.
- Intentional changes under ADR 0007/0012: singleton output shape, PHP threshold `92`, fuzzy-title automatic clustering, no-id runtime identity, and pre-collapsed PHP corpus input.
- Intentional non-adoption under ADR 0026: PHP locked-Deduplication rejection and snapshot-dependent citable export metadata.
- No C# production defect was demonstrated by the fixture set.

## Exit Condition

Phase 7 is complete only when H25-H29 are complete and every retained compatibility statement names its fixture set and comparison result. Uncovered behavior remains explicitly unclaimed.

H29 status:

- H29 generated fixtures are evidence-only and are explicitly scoped as non-implementation.
- `PhpCompatibilityEvidenceClosureTests` validates H29 provenance and PHP observations plus the aggregate Phase 7 inventory; it does not fabricate a C# semantic replay target.
- Retained H29 statements are constrained to: "evidence generated for citation network and dissemination-export behavior under ADR 0027", and all broader PHP compatibility claims remain unasserted.
