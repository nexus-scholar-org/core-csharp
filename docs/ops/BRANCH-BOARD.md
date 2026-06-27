# Codex Branch Board

Source: live branch probes from local `main` after the Gate 9 Search reconnaissance merge.

## Main Baseline

- Current `main` head: `3688ca16bc03f1fe5f86096e810ffebf97d0f2dd` (`docs: map PHP search behavior`).
- Gate 0 through Gate 6 are merged into `main`.
- Gate 9 shared identity is merged into `main`; Gate 9 was intentionally started before Gate 6.
- Gate 9 Search reconnaissance is merged into `main` as docs/planning only.
- Gate 9 Search recon branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28285488732`.
- Gate 9 Search recon push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28285547851`.
- Search implementation readiness remains `No`.

## Branch Classes

- merged: `main`, `cdx/gate-9-search-recon`, `cdx/gate-6-bundle-planning`, `cdx/gate-9-shared-identity`, `cdx/gate-5-provenance`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/shared-identity-adr-0007`, `cdx/run-gate-zero-discovery`, `cdx/run-gate-0-discovery`
- cleanup: `cdx/gate-9-search-recon`, `cdx/gate-6-bundle-planning`, `cdx/gate-9-shared-identity`, `cdx/gate-5-provenance`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/run-gate-zero-discovery`
- active: `main`
- blocked: Search implementation, provider/network behavior, PHP compatibility claims, generated PHP fixtures, and Deduplication/Search integration remain blocked until ADR 0010 resolves the Search trace and plan contract.
- stale: `cdx/run-gate-0-discovery`, `cdx/main-gate2-merge`
- review: none identified by current git state

Cleanup candidates above are confirmed by `git branch --merged main`.

## Safe Cleanup Candidates

- `cdx/gate-9-search-recon`
- `cdx/gate-6-bundle-planning`
- `cdx/gate-9-shared-identity`
- `cdx/gate-5-provenance`
- `cdx/two-model-codex-workflow`
- `cdx/main-gate2-merge`
- `cdx/gate-4-workflow`
- `cdx/gate-4-workflow-planning`
- `cdx/gate-3-protocol-lifecycle`
- `cdx/gate-3-planning-decisions`
- `cdx/gate-2-digest-kernel-cleanup`
- `cdx/run-gate-zero-discovery`

## Not Safe To Delete

- `main`
- `cdx/run-gate-0-discovery`
- `cdx/app-recon-cli-web-core-usage` until its owner/purpose is classified

## Next Work

- Next active branch target: `cdx/gate-9-search-contract`.
- Goal: ADR 0010 Search Trace and Plan Contract.
- Scope: decide C# Search trace/result shape, raw provider sighting preservation, no Search-time deduplication, deterministic year clock, `includeRawData` cache-key policy, PHP-permissive versus schema-closed plan parsing, stub-provider-only first implementation, and Search-to-Deduplication handoff.
- Do not implement Search, providers, network behavior, persistence, API/UI/cloud, PHP compatibility, generated PHP fixtures, or blueprint conformance in the contract branch unless a new prompt explicitly changes scope.

## Unresolved Ambiguity

- `cdx/run-gate-0-discovery` is still retained for historical reference.
- `cdx/app-recon-cli-web-core-usage` is merged by containment but not classified as a gate/process cleanup branch.
