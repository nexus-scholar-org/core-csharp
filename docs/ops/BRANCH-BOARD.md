# Codex Branch Board

Source: live branch probes after public-feedback onboarding, `gh-pages` first-tester walkthrough, PR #6 UI presentation merge, and stale remote branch cleanup.

## Main Baseline

- Operating baseline: `main` / `origin/main` at `ac0307c` (`Polish Avalonia sample host presentation and scrolling`).
- Public site branch: `gh-pages` / `origin/gh-pages` at `32475f4` (`docs(site): add first tester getting started walkthrough`).
- Remote heads are only `main` and `gh-pages`.
- Local durable heads are `main` and `gh-pages`.
- This state-refresh branch, `cdx/state-refresh-public-comprehension`, is docs-only and exists to align stale ops/review files with the current baseline.
- Latest hosted `main` CI: `gate-01` run `28402404840`, passed for `ac0307c` on Ubuntu and Windows.

## Main Contains

- Gate 0 through Gate 6 local foundations.
- Gate 9 Shared Identity.
- Gate 9 Search and Search Import.
- Gate 9 Deduplication.
- Gate 9 Screening.
- Gate 9 Full Text reconnaissance, ADR 0014 contract, and local no-network Full Text implementation.
- UI contracts and sample block plans.
- Avalonia block renderer prototype.
- Avalonia sample host with presentation and scrolling polish.
- Public-feedback onboarding docs, issue templates, PR template, and deterministic CLI `demo`.
- README quick start for `doctor`, `sample`, `demo`, and the sample host.

## Public Site Contains

- First-tester getting-started walkthrough on `gh-pages`.
- Public site pages for project narrative, architecture, module documentation, and tutorials.
- The remaining public-site gap is a fresh sample-host screenshot or GIF linked from the walkthrough.

## Branch Classes

- merged: all prior implementation and public-feedback `cdx/*` branches needed for the current baseline have been merged, squash-merged, cherry-picked, or superseded into `main`.
- cleanup: none pending locally or remotely.
- active: docs-only state refresh on `cdx/state-refresh-public-comprehension`.
- review: none yet.
- blocked: PHP compatibility claims, generated PHP fixtures, persistence/API/cloud, live provider/network behavior, Scopus API, Web of Science API, Google Scholar scraping, paywall bypass, shadow-library sources, AI governance beyond proposal contracts, full-text artifact storage, actual PDF parsing, OCR, and product-shell behavior remain out of scope.
- stale: none retained locally or remotely.
- public-site: `gh-pages`.

## Remote Cleanup State

`git ls-remote --heads origin`:

```text
32475f4e5fc2bf5b33becfff02d9607f16016fda refs/heads/gh-pages
ac0307c7a5e396c36325c42632a02b2faca10172 refs/heads/main
```

Branches deleted during the public-feedback cleanup:

- `cdx/gate-9-fulltext-contract`
- `cdx/gate-9-fulltext-recon`
- `cdx/public-feedback-cli-onboarding`
- `cdx/ui-phase-3-5-avalonia-sample-host`
- `cdx/ui-phase-3-avalonia-renderer`
- `cdx/ui-presentation-pass`

## Safe Cleanup Candidates

None.

## Not Safe To Delete

- `main`
- `gh-pages`
- `origin/main`
- `origin/gh-pages`

## Completed Public-Feedback Packets

- PF-01 maintainer routing docs.
- PF-02 GitHub issue templates and PR template.
- CLI-01 local CLI demo contract.
- CLI-02 deterministic local `demo` command.
- CLI-03 README and public-readiness demo docs.
- WEB-01 public first-tester getting-started tutorial on `gh-pages`.
- UI presentation pass for the Avalonia sample host, merged as PR #6.

## Current Recommended Next Work

1. Finish this state refresh so ops/review docs match `main` and `gh-pages`.
2. Add first-tester polish: sample-host screenshot/GIF on `gh-pages`, root contributor/license/security docs, and README links to the public tutorial and feedback templates.
3. Add CI or script smoke coverage for the public CLI path: `doctor`, `sample`, and `demo`.
4. Plan APP-01 as a narrow ADR for read-only AppServices composition from Search Import + Deduplication into `WorkspacePlan`.
5. Keep provider work planning-only until a provider/network/legal ADR exists.

## Unresolved Boundaries

- `CF-025`: implemented/resolved for local Full Text artifact evidence; preserve exact raw bytes plus `raw-artifact-bytes` digest identity.
- `CF-026`: narrowed; live provider/network and legal-access behavior remain future.
- `CF-027`: narrowed for Core; app rows and paths remain projections unless transformed into Core Full Text records.
- `CF-024`: Screening app workflow rows remain projections.
- `CF-019`: Search import remaining parser families and live provider/API integration remain future.
