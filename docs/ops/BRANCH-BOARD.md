# Branch Board

Status date: 2026-07-14

## Current Repository State

- `main`: Phase 7 complete through Hardening 29 at merge `37df601` (PR #53).
- `gh-pages`: retained as historical static-site source at `9a76975`; new Pages deployments are sourced from `site/` on `main`.
- Active corrective branch: `cdx/hardening-post-phase-7-remediation` (Hardening 30).
- Active plan: `docs/reviews/2026-07-11-hardening-plan/README.md`.
- Feature expansion remains frozen.

## Completed Hardening

- Phases 1-7 are complete on protected `main`.
- Phase 6 has accepted release policy, a 12-package validation topology, 30 locked solution restore graphs, normalized package reproducibility, clean local-source package smoke, SPDX SBOM and release evidence, retained test artifacts, dependency review, CodeQL SARIF, and validation-only artifact attestation.
- Current protected-main verification baseline is 561 tests on Windows and Linux.

## Current Closeout

- Pages is built through workflow run `29231264071`.
- `main`, private reporting, security analysis, and the tag-only `release` environment pass `scripts/verify-github-governance.ps1`.
- Tag `v0.1.0-alpha.1` completed attested clean-machine run `29231634501`.
- Phase 7 fixture-backed compatibility evidence landed through PRs #49-#53 with case-scoped claims only.
- Hardening 30 addresses the post-phase review defects in AI proposal authority, Full Text chain validation, BibTeX author parsing, H28 evidence guards, package version identity, and status documentation.
- The active validation package identity is `0.1.0-alpha.2`; `v0.1.0-alpha.1` remains the historical attested rehearsal and no NuGet package is published.

## Product Boundary

No live providers, scraping, persistence/API/cloud, PDF/OCR, product UI shell, package publication, signing, executable merge decisions, or PHP compatibility claims are authorized.

## Protected References

- `main`
- `gh-pages` historical branch
- `origin/main`
- `origin/gh-pages`
