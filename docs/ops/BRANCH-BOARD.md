# Codex Branch Board

Source: live branch probes from local `main` after the Gate 9 Screening reconnaissance merge.

## Main Baseline

- Latest Gate 9 Screening reconnaissance merge on `main`: `095a275` (`docs: map PHP screening behavior`); later ops-only commits may sit on top.
- Gate 0 through Gate 6 are merged into `main`.
- Gate 9 shared identity is merged into `main`.
- Gate 9 Search reconnaissance is merged into `main` as docs/planning only.
- ADR 0010 Search Trace and Plan Contract is merged into `main`.
- Gate 9 Search local stub-provider implementation is merged into `main`.
- ADR 0011 Search Import Source Contract is merged into `main`.
- Gate 9 Search import local first-slice parser implementation is merged into `main`.
- Gate 9 Deduplication reconnaissance is merged into `main` as docs/planning only.
- ADR 0012 Deduplication Evidence and Cluster Contract is merged into `main`.
- Gate 9 Dedup local implementation is merged into `main`.
- Gate 9 Screening reconnaissance is merged into `main` as docs/planning only.
- Gate 9 Screening recon branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28299101552`.
- Gate 9 Screening recon push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28299166389`.
- Local C# Dedup behavior is implemented for ADR 0012 scope only.
- PHP compatibility, generated PHP fixtures, Screening implementation, persistence/API/UI/cloud, live provider/network behavior, AI governance, and app behavior as Core authority remain unclaimed.

## Branch Classes

- merged: `main`, `cdx/gate-9-screening-recon`, `cdx/gate-9-dedup-local`, `cdx/gate-9-dedup-contract`, `cdx/gate-9-dedup-recon`, `cdx/gate-9-search-import-local`, `cdx/gate-9-search-import-contract`, `cdx/gate-9-search-local`, `cdx/gate-9-search-contract`, `cdx/app-recon-cli-web-core-usage`, `cdx/gate-9-search-recon`, `cdx/gate-6-bundle-planning`, `cdx/gate-9-shared-identity`, `cdx/gate-5-provenance`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/shared-identity-adr-0007`, `cdx/run-gate-zero-discovery`, `cdx/run-gate-0-discovery`
- cleanup: no merged GitHub remote branch cleanup candidates remain; local historical branches listed above may be pruned locally when desired.
- active: none in the local branch graph after this refresh
- review: none
- blocked: PHP compatibility claims, generated PHP fixtures, C# Screening implementation, persistence/API/UI/cloud, live provider/network behavior, Scopus API, Web of Science API, Google Scholar scraping, AI governance, and app integration claims remain out of scope.
- stale: none newly identified beyond merged historical branches.

Remote cleanup state from `git branch -r`: only `origin/main` remains.

## Safe Cleanup Candidates

- none on GitHub

## Not Safe To Delete

- `main`

## Next Work

- Next branch: `cdx/gate-9-screening-contract`.
- Goal: write ADR 0013 Screening Decision and Conflict Contract.
- Focus areas: Screening input shape, stage model, criteria digest, decision shape, human authority, AI proposal boundary, conflict detection, adjudication, locked candidate set binding, provenance relation, fixture families, and comparator strategy.
- Do not add C# Screening implementation, persistence/API/UI/cloud, live provider/network behavior, PHP-generated fixtures, PHP compatibility claims, CLI/Web changes, AI governance implementation, or app behavior as Core authority.

## Unresolved Boundaries

- `CF-011`: implemented for local Dedup input shape; PHP compatibility pending.
- `CF-012`: implemented for local threshold `95` / `0.95`; PHP compatibility pending.
- `CF-016`: implemented for Search and local Dedup handoff without changing Search behavior.
- `CF-018`: remains narrowed for app consumer boundary.
- `CF-019`: implemented for local first-slice Search import parser behavior only; remaining parser families, live APIs, source-specific namespace expansion, PHP compatibility, and app alignment remain future.
- `CF-020`: narrowed by ADR 0012; Web hashes, snapshots, persisted runs, and app scoring remain app projections, not Core authority.
- `CF-021`: open/blocking; Screening input and locked candidate boundary must be resolved by ADR 0013.
- `CF-022`: open/blocking; Screening decision authority and AI proposal boundary must be resolved by ADR 0013.
- `CF-023`: open/blocking; Screening criteria schema and digest contract must be resolved by ADR 0013.
- `CF-024`: important; Screening app workflow projection boundary must be narrowed by ADR 0013.
