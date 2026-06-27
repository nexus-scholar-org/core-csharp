# Codex Branch Board

Source: live branch probes from local `main` after the ADR 0012 Deduplication Contract merge.

## Main Baseline

- Current product `main` head: `0249f67` (`docs: define deduplication contract`).
- Gate 0 through Gate 6 are merged into `main`.
- Gate 9 shared identity is merged into `main`.
- Gate 9 Search reconnaissance is merged into `main` as docs/planning only.
- ADR 0010 Search Trace and Plan Contract is merged into `main`.
- Gate 9 Search local stub-provider implementation is merged into `main`.
- ADR 0011 Search Import Source Contract is merged into `main`.
- Gate 9 Search import local first-slice parser implementation is merged into `main`.
- Gate 9 Deduplication reconnaissance is merged into `main` as docs/planning only.
- ADR 0012 Deduplication Evidence and Cluster Contract is merged into `main`.
- ADR 0012 branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28294430050`.
- ADR 0012 push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28294876256`.
- Deduplication implementation is now ready only for local C# behavior against ADR 0012.
- PHP compatibility, generated PHP fixtures, Screening, persistence/API/UI/cloud, live provider/network behavior, and app behavior as Core authority remain unclaimed.

## Branch Classes

- merged: `main`, `cdx/gate-9-dedup-contract`, `cdx/gate-9-dedup-recon`, `cdx/gate-9-search-import-local`, `cdx/gate-9-search-import-contract`, `cdx/gate-9-search-local`, `cdx/gate-9-search-contract`, `cdx/app-recon-cli-web-core-usage`, `cdx/gate-9-search-recon`, `cdx/gate-6-bundle-planning`, `cdx/gate-9-shared-identity`, `cdx/gate-5-provenance`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/shared-identity-adr-0007`, `cdx/run-gate-zero-discovery`, `cdx/run-gate-0-discovery`
- cleanup: `origin/cdx/gate-9-dedup-contract` and `origin/cdx/gate-9-dedup-recon` are merged remote cleanup candidates; local historical branches listed above may be pruned locally when desired.
- active: none in the local branch graph after this refresh
- review: none
- blocked: PHP compatibility claims, generated PHP fixtures, Screening, persistence/API/UI/cloud, live provider/network behavior, Scopus API, Web of Science API, Google Scholar scraping, and app integration claims remain out of scope.
- stale: none newly identified beyond merged historical branches and remote cleanup candidates.

Remote cleanup state from `git branch -r`: `origin/main`, `origin/cdx/gate-9-dedup-contract`, and `origin/cdx/gate-9-dedup-recon`.

## Safe Cleanup Candidates

- `origin/cdx/gate-9-dedup-contract`
- `origin/cdx/gate-9-dedup-recon`

## Not Safe To Delete

- `main`

## Next Work

- Next branch: `cdx/gate-9-dedup-local`.
- Goal: implement local C# Deduplication behavior against ADR 0012.
- Focus areas: raw Search/import sighting input, exact ADR 0007 identifier clustering, fuzzy-title review-required candidates with local threshold `95` / `0.95`, no-id unresolved candidates, deterministic representative election, preserved evidence links, and app projection boundary.
- Do not add Screening, persistence/API/UI/cloud, live provider/network behavior, import parser changes, PHP-generated fixtures, PHP compatibility claims, CLI/Web changes, or Search behavior changes.

## Unresolved Boundaries

- `CF-011`: resolved by ADR 0012 for the local contract; implementation pending.
- `CF-012`: resolved by ADR 0012 for local threshold `95` / `0.95`; PHP compatibility pending.
- `CF-016`: implemented for Search and narrowed for Dedup handoff by ADR 0012.
- `CF-018`: remains narrowed for app consumer boundary.
- `CF-019`: implemented for local first-slice Search import parser behavior only; remaining parser families, live APIs, source-specific namespace expansion, PHP compatibility, and app alignment remain future.
- `CF-020`: narrowed by ADR 0012; Web hashes, snapshots, persisted runs, and app scoring remain app projections, not Core authority.
