# Merge Queue

Source: live status from branch probes after the ADR 0012 Deduplication Contract merge.

## Completed Merges

- `cdx/run-gate-zero-discovery` (merged to `main`)
- `cdx/gate-2-digest-kernel-cleanup` (merged to `main`)
- `cdx/gate-3-planning-decisions` (merged to `main`)
- `cdx/gate-3-protocol-lifecycle` (merged to `main`)
- `cdx/gate-4-workflow-planning` (merged to `main`)
- `cdx/gate-4-workflow` (merged to `main`)
- `cdx/gate-5-provenance` (merged to `main`)
- `cdx/gate-9-shared-identity` (merged to `main`)
- `cdx/gate-6-bundle-planning` (merged to `main`)
- `cdx/gate-9-search-recon` (merged to `main`)
- `cdx/app-recon-cli-web-core-usage` (merged to `main`)
- `cdx/gate-9-search-contract` (merged to `main`)
- `cdx/gate-9-search-local` (merged to `main`)
- `cdx/gate-9-search-import-contract` (merged to `main`)
- `cdx/gate-9-search-import-local` (merged to `main`)
- `cdx/gate-9-dedup-recon` (merged to `main`)
- `cdx/gate-9-dedup-contract` (merged to `main`)
- `cdx/two-model-codex-workflow` (historical merged workflow setup branch)
- `cdx/shared-identity-adr-0007` (historical planning branch)

## Current Queue

- `main` includes ADR 0012 Deduplication Evidence and Cluster Contract at `0249f67`.
- ADR 0012 branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28294430050`.
- ADR 0012 push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28294876256`.
- GitHub remote branch cleanup candidates: `origin/cdx/gate-9-dedup-contract`, `origin/cdx/gate-9-dedup-recon`.
- Next primary branch: `cdx/gate-9-dedup-local`.
- Local Dedup implementation should use ADR 0012 and preserve the explicit non-claims.

## Not Queued Yet

- remaining imported-export parser families beyond RIS, BibTeX, and Scopus CSV/export
- live provider/network calls
- Scopus API
- Web of Science API
- Google Scholar scraping
- PHP compatibility claims
- generated PHP fixtures
- Screening behavior
- persistence/API/UI/cloud behavior
- CLI/Web app alignment

## Cleanup Candidates

- `origin/cdx/gate-9-dedup-contract`
- `origin/cdx/gate-9-dedup-recon`

## Not Safe To Delete

- `main`

## Verification

- `git branch --merged main` includes `cdx/gate-9-dedup-contract` and `cdx/gate-9-dedup-recon`.
- `git branch -r` returns `origin/main`, `origin/cdx/gate-9-dedup-contract`, and `origin/cdx/gate-9-dedup-recon` as the remaining remote branches after this refresh.
