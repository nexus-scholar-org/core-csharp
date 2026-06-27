# Merge Queue

Source: live status from branch probes after the Gate 9 Dedup local implementation merge.

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
- `cdx/gate-9-dedup-local` (merged to `main`)
- `cdx/two-model-codex-workflow` (historical merged workflow setup branch)
- `cdx/shared-identity-adr-0007` (historical planning branch)

## Current Queue

- `main` includes Gate 9 Dedup local implementation at `8fa573d`.
- Gate 9 Dedup branch CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28298203837`.
- Gate 9 Dedup push-triggered `main` CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28298275746`.
- GitHub remote branch cleanup candidates: none.
- Next primary branch: `cdx/gate-9-screening-recon`.
- Screening work should begin as PHP behavior reconnaissance and fixture/comparator planning only.

## Not Queued Yet

- C# Screening implementation
- remaining imported-export parser families beyond RIS, BibTeX, and Scopus CSV/export
- live provider/network calls
- Scopus API
- Web of Science API
- Google Scholar scraping
- PHP compatibility claims
- generated PHP fixtures
- persistence/API/UI/cloud behavior
- CLI/Web app alignment

## Cleanup Candidates

- none on GitHub

## Not Safe To Delete

- `main`

## Verification

- `git branch --merged main` includes `cdx/gate-9-dedup-local`, `cdx/gate-9-dedup-contract`, and `cdx/gate-9-dedup-recon`.
- `git branch -r` returns only `origin/main` after this refresh.
