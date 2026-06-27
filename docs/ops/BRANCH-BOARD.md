# Codex Branch Board

Source: live branch probes from local `main` after the Gate 9 Deduplication reconnaissance merge.

## Main Baseline

- Current product `main` head: `76933e3` (`ci: trigger dedup recon verification`).
- Gate 0 through Gate 6 are merged into `main`.
- Gate 9 shared identity is merged into `main`.
- Gate 9 Search reconnaissance is merged into `main` as docs/planning only.
- ADR 0010 Search Trace and Plan Contract is merged into `main`.
- Gate 9 Search local stub-provider implementation is merged into `main`.
- ADR 0011 Search Import Source Contract is merged into `main`.
- Gate 9 Search import local first-slice parser implementation is merged into `main`.
- Gate 9 Deduplication reconnaissance is merged into `main` as docs/planning only.
- ADR 0010 branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28289131170`.
- ADR 0010 push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28289224733`.
- Gate 9 Search local branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28290113371`.
- Gate 9 Search push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28290167673`.
- ADR 0011 branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28290630584`.
- ADR 0011 push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28290718641`.
- Gate 9 Search import-local final branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28291884081`.
- Gate 9 Search import-local push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28291938166`.
- Gate 9 Deduplication reconnaissance branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28293730505`.
- Gate 9 Deduplication reconnaissance push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28293796105`.
- GitHub remote branch cleanup is no longer complete because `origin/cdx/gate-9-dedup-recon` still exists after merge.
- Search local implementation is complete only for deterministic stub-provider Search traces.
- Imported-export parser implementation is complete only for the local first slice: RIS, BibTeX, Scopus CSV/export, exact source-file digest binding, local import actor, parser warnings, skipped-record evidence, no Search-time Deduplication, and no source-specific WorkId namespace promotion.
- Deduplication reconnaissance is complete only for PHP behavior mapping and fixture/comparator planning. C# Deduplication implementation remains blocked pending ADR 0012.

## Branch Classes

- merged: `main`, `cdx/gate-9-dedup-recon`, `cdx/gate-9-search-import-local`, `cdx/gate-9-search-import-contract`, `cdx/gate-9-search-local`, `cdx/gate-9-search-contract`, `cdx/app-recon-cli-web-core-usage`, `cdx/gate-9-search-recon`, `cdx/gate-6-bundle-planning`, `cdx/gate-9-shared-identity`, `cdx/gate-5-provenance`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/shared-identity-adr-0007`, `cdx/run-gate-zero-discovery`, `cdx/run-gate-0-discovery`
- cleanup: `origin/cdx/gate-9-dedup-recon` is a merged remote cleanup candidate; local historical branches listed above may be pruned locally when desired.
- active: none in the local branch graph after this refresh
- review: none
- blocked: live provider/network behavior, Scopus API, Web of Science API, Google Scholar scraping, PHP compatibility claims, generated PHP fixtures, Deduplication, Screening, persistence/API/UI/cloud, and app integration claims remain out of scope.

Remote cleanup state from `git branch -r`: `origin/main` and merged cleanup candidate `origin/cdx/gate-9-dedup-recon`.

## Safe Cleanup Candidates

- `origin/cdx/gate-9-dedup-recon`

## Not Safe To Delete

- `main`

## Next Work

- Next branch: `cdx/gate-9-dedup-contract`.
- Goal: write ADR 0012 Deduplication Evidence and Cluster Contract.
- Focus areas: resolve `CF-011` raw Dedup input shape, `CF-012` fuzzy threshold, `CF-020` app projection boundary, and narrow `CF-016` only for Search-to-Dedup handoff.
- Do not implement C# Deduplication, generate fixtures, add Screening, add persistence/API/UI/cloud, change CLI/Web, or claim PHP compatibility in the contract branch.

## Unresolved Boundaries

- `CF-013`: implemented for local Search cache identity.
- `CF-016`: implemented for raw Search trace and no-Dedup boundary.
- `CF-017`: implemented for local schema-closed Search plans.
- `CF-018`: remains narrowed for app consumer boundary.
- `CF-019`: implemented for local first-slice Search import parser behavior only; remaining parser families, live APIs, source-specific namespace expansion, PHP compatibility, and app alignment remain future.
- `CF-011`: raw duplicate input shape is blocking for Deduplication implementation and must be resolved by ADR 0012.
- `CF-012`: title fuzzy threshold conflict is blocking for Deduplication implementation and must be resolved by ADR 0012.
- `CF-020`: Deduplication app projection and representative snapshot boundary is open and must be resolved or narrowed by ADR 0012.
