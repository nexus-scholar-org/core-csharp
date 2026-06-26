# Codex Branch Board

Source: live Git state in `C:\Users\mouadh\Documents\AI in research\core-csharp`.

## Active branches
- `cdx/gate-3-protocol-lifecycle` (current) @ `a8b9f68`: docs: record gate 3 hosted ci evidence
- `cdx/gate-3-planning-decisions` @ `d925796`: docs: define gate 3 protocol planning decisions
- `cdx/gate-2-digest-kernel-cleanup` @ `5e5dde1`: refactor: move digest primitives into kernel
- `cdx/run-gate-zero-discovery` @ `e17ec4f`: docs: record gate 2 hosted ci evidence
- `cdx/run-gate-0-discovery` @ `ee46eb4`: Initial Nexus Scholar Core C# scaffold
- `main` @ `ee46eb4`: Initial Nexus Scholar Core C# scaffold

## Gate 3 planning containment status
- `d925796` (Gate 3 planning decision) is contained in `cdx/gate-3-protocol-lifecycle`.
- `d925796` is also in `cdx/gate-3-planning-decisions` (self).
- `cdx/gate-3-protocol-lifecycle` therefore already contains Gate 3 planning work.

## Merge-ready candidates
- All listed branch tips are present on `cdx/gate-3-protocol-lifecycle` via `d925796`, `5e5dde1`, `e17ec4f`, and `a8b9f68`.
- `cdx/gate-2-digest-kernel-cleanup` and `cdx/gate-3-planning-decisions` are merge-ready behind `cdx/gate-3-protocol-lifecycle` because `git branch --contains` shows protocol-lifecycle contains both commit sets.
- `git branch --merged` relative to `cdx/gate-3-protocol-lifecycle` reports no remaining unmerged local branches.

## Merge-ready to `origin/main`
- `cdx/gate-3-protocol-lifecycle` is the only branch carrying the current Gate 3 protocol lifecycle line for final merge.
- `git branch --merged origin/main` currently shows only `cdx/run-gate-0-discovery` and `main`, which means newer branches are not yet in the `origin/main` baseline.

## Local-only branches
- `cdx/run-gate-0-discovery` (no local upstream in `git branch -vv` output).
- `main` (no local tracking entry in `git branch -vv` output).

## Stale branch candidates
- `cdx/run-gate-0-discovery` is stale by age and has no new commits beyond `ee46eb4`.
- `main` is currently at `ee46eb4` and not moved by the recent Gate 2/3 work.
- `cdx/run-gate-0-discovery` and `main` are older than all active Gate 2/3 line branches.

## Containment summary by key commit
- `d925796`: contained by `cdx/gate-3-planning-decisions`, `cdx/gate-3-protocol-lifecycle`
- `5e5dde1`: contained by `cdx/gate-2-digest-kernel-cleanup`, `cdx/gate-3-planning-decisions`, `cdx/gate-3-protocol-lifecycle`
- `a8b9f68`: contained by `cdx/gate-3-protocol-lifecycle`
- `e17ec4f`: contained by `cdx/gate-2-digest-kernel-cleanup`, `cdx/gate-3-planning-decisions`, `cdx/gate-3-protocol-lifecycle`, `cdx/run-gate-zero-discovery`
