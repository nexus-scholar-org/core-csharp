# Branch Board

Status date: 2026-07-19

## Protected Main

- Alpha.2 release commit: `9658639`.
- Alpha.2 release authority: `v0.1.0-alpha.2` plus
  `desktop-distribution-manifest.json`.
- `gh-pages`: retained as historical static-site source; deployments use
  `site/` on `main`.
- Release branches from PRs #72 and #73 are merged and deleted remotely.
- FE-10 design has not started.
- Active roadmap: `docs/plans/2026-07-14-feature-expansion-priority.md`.

## Verified Baseline

- Hardening Phases 1-7 and Hardening 30: complete.
- FE-01 through FE-08: complete within accepted local scopes; FE-08 slices 1 through 9 complete.
- FE-09A through FE-09F: complete within accepted scope and merged through
  PR #69.
- Public Astro Pages baseline: merged through PR #70.
- Release full solution: 1,111 passed, 0 failed, 4 expected skips.
- ADR 0044 and ADR 0045 were historical integrity work and remain historical evidence.
- Remote governance limitation: one repository collaborator cannot supply an
  independent GitHub approval; main also lacks required CODEOWNER review,
  latest-push reapproval, linear history, and signed-commit enforcement. This
  limits governance claims but does not bypass the configured protected-main
  checks or the alpha.2 release gate.
- Package graph: 24 validation-only packages with reproducible pack and clean
  local-source restore/load.
- Release build and formatting: green.
- Package identity: `0.1.0-alpha.2`; NuGet publication disabled.
- Desktop identity: unsigned self-contained Windows x64 GitHub prerelease.

## Product Boundary

Scientific authority remains in immutable, digest-bound Core records and local
workspace generations. The desktop and provider hosts invoke admitted commands;
they do not own scientific authority.

FE-09 admits bounded Search transport, provider evidence caching, recorded Full
Text retrieval verification, and local citation snapshots. Semantic Scholar
body retention remains digest-only by default. Live Full Text transport,
scraping, paywall bypass, citation exports, PHP parity, plugin execution,
database/API/cloud, authentication, tenancy, and multi-user operation remain
outside the accepted scope.

## Release Readiness Alpha 2

- Status: complete and published as
  [`v0.1.0-alpha.2`](https://github.com/nexus-scholar-org/core-csharp/releases/tag/v0.1.0-alpha.2).
- Contract: ADR 0046 and accepted release gate.
- Artifact: deterministic-inventory portable ZIP, manifest, checksums, SPDX
  SBOM, and GitHub attestation.
- Runtime resilience: bounded sanitized local diagnostics.
- Recovery: lock-aware manifest backup and byte-exact new-directory restore.
- Acceptance: real Avalonia headless workflow, automation, focus, and scaling.
- Publication: exact matching protected-main tag only; five assets published,
  checksummed, attested, downloaded, and independently verified.

## Next

FE-10 design and capability-security review is next. Runtime implementation
remains blocked behind its own accepted ADR and gate.

## Pages

- GitHub Pages source: Astro project under `site/` on `main`.
- Generated deployment artifact: `site/dist/` in CI only; never committed.
- Deployment workflow: `.github/workflows/pages.yml`.
- `gh-pages` is retained only as historical branch state.
