# Merge Queue

Source: live status from `git branch -vv`, `git branch --merged main`, and `git branch -r --merged origin/main`.

## Completed Merges

- `cdx/run-gate-zero-discovery`
- `cdx/gate-2-digest-kernel-cleanup`
- `cdx/gate-3-planning-decisions`
- `cdx/gate-3-protocol-lifecycle`
- `cdx/two-model-codex-workflow`

All five are merged to local `main`. Remote merge state also shows `origin/cdx/two-model-codex-workflow` merged to `origin/main`.

## Current Queue

- `main` is current at `467d5f2`.
- Gate 0 through Gate 3 are closed into the `main` baseline.
- Gate 4 is the next planning work.

## Blocked Implementation

- Do not start Gate 4 implementation until `CF-003`, `CF-006`, and `CF-007` are resolved.
- PHP Shared identity reconnaissance is allowed only as docs-only parallel work while Gate 4 remains planning-blocked.

## Cleanup Candidates

- `cdx/two-model-codex-workflow`
- `cdx/main-gate2-merge`
- `cdx/gate-3-protocol-lifecycle`
- `cdx/gate-3-planning-decisions`
- `cdx/gate-2-digest-kernel-cleanup`
- `cdx/run-gate-zero-discovery`

## Not Safe To Delete

- `main`
- `cdx/run-gate-0-discovery`

## Unresolved Ambiguity

- Local `main` is not tracking `origin/main`, so normal `git pull` does not work until upstream is configured or the remote/branch is passed explicitly.
