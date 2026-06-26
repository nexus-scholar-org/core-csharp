# Codex Branch Board

Source: post-merge branch probes run from local `main` after the manual GitHub merge of `cdx/two-model-codex-workflow`.

## Main Baseline

- Current `main` commit: `467d5f2` (`Merge pull request #1 from nexus-scholar/cdx/two-model-codex-workflow`).
- Gate 0 through Gate 3 are merged into `main`.
- `cdx/two-model-codex-workflow` is merged into `main`.

## Branch Classes

- merged: `main`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/run-gate-zero-discovery`, `cdx/run-gate-0-discovery`
- cleanup: `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/run-gate-zero-discovery`
- active: `main`
- blocked: Gate 4 implementation branch work until `CF-003`, `CF-006`, and `CF-007` are resolved
- stale: `cdx/run-gate-0-discovery`, `cdx/main-gate2-merge`
- review: none identified by current git state

Cleanup candidates above are confirmed by `git branch --merged main`. Remote merge state also confirms `origin/cdx/two-model-codex-workflow` is merged into `origin/main`.

## Safe Cleanup Candidates

- `cdx/two-model-codex-workflow`
- `cdx/main-gate2-merge`
- `cdx/gate-3-protocol-lifecycle`
- `cdx/gate-3-planning-decisions`
- `cdx/gate-2-digest-kernel-cleanup`
- `cdx/run-gate-zero-discovery`

## Not Safe To Delete

- `main`
- `cdx/run-gate-0-discovery`

`cdx/run-gate-0-discovery` is stale, but it is not listed as a preferred cleanup candidate in this board refresh because it remains the root local bootstrap lane and may still be useful for audit recovery.

## Next Work

- Gate 4 is the next planning work.
- Gate 4 implementation is blocked until `CF-003`, `CF-006`, and `CF-007` are resolved.
- PHP Shared identity reconnaissance is allowed only as docs-only parallel work.

## Unresolved Ambiguity

- Local `main` still has no configured upstream tracking branch, so plain `git pull` failed and refresh required `git pull origin main`.
