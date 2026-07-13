# Hardening 26 - Search Compatibility Evidence

Status: complete; protected-branch merge verified by PR #50 at merge `24f302d`.

## Behavior Implemented

- Generates deterministic Search fixtures from the PHP commit pinned by `specs/SOURCE.lock.json`.
- Compares query and year validation, provider-alias normalization, cache relations, PHP legacy-plan import, provider selection order, and partial/all failure behavior.
- Classifies three ADR 0010 intentional changes: `include_raw_data` cache identity, schema-closed authoritative plans, and preservation of raw sightings instead of PHP Search-time deduplication.
- Corrects C# upper-bound validation for `year_from`.
- Corrects C# lower-bound validation for `year_to`.
- Normalizes exceptions thrown by provider execution into failed attempts so later providers still run.
- Keeps provider-result normalization and trace-construction defects outside the provider-failure boundary.

## Evidence

- Generator: `scripts/php-golden/search-export.php`
- Fixture set: `fixtures/php-golden/search/v1/`
- Comparator: `tests/NexusScholar.Conformance.Tests/PhpSearchGoldenTests.cs`
- PHP source: `nexus-scholar/core@b24d0d71ec7b64003465182477e7edb7f49994f4`
- Cases: 18 total; 15 equivalent semantic cases and 3 intentional changes.

## Invariants Enforced

- Generation refuses a mismatched commit or dirty tracked PHP worktree.
- Input, output, source lock, and classification bytes are SHA-256 bound by the generated manifest.
- Every PHP case has exactly one reviewed classification.
- The complete 18-case inventory and exact three-case intentional-change inventory are pinned by conformance tests.
- Source references, generator command, environment assumptions, ignored nondeterminism, and comparison rules are pinned by conformance tests.
- H26 fails when a case is classified as a C# defect or unresolved specification conflict.
- Cache comparison preserves field-sensitivity claims without falsely claiming byte-identical PHP/C# cache keys.
- Runtime query ids, provider durations, network behavior, and app persistence are excluded.

## Explicit Non-Claims

- No live-provider, HTTP, retry, rate-limit, credential, SDK, or cassette parity.
- No default PHP Laravel adapter-registration-order parity with the local C# stub catalog.
- No exact PHP/C# cache-key byte parity.
- No query outer-whitespace storage parity.
- No imported-export PHP compatibility; ADR 0011 local RIS/BibTeX/Scopus CSV behavior has no corresponding pinned PHP Search surface.
- No Laravel persistence, jobs, commands, API, UI, or cloud compatibility.

## ADR And PHP Impact

- No new ADR is required. ADR 0010 already decides the three intentional Search differences and deterministic year policy.
- ADR 0011 remains local imported-export authority; H26 does not convert it into a PHP compatibility claim.
- PHP behavior remains evidence only. C# Search continues to return raw Search traces and never calls Deduplication.
