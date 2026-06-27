# Merge Queue

Source: live status from branch probes after Gate 9 merge.

## Completed Merges

- `cdx/run-gate-zero-discovery` (merged to `main`)
- `cdx/gate-2-digest-kernel-cleanup` (merged to `main`)
- `cdx/gate-3-planning-decisions` (merged to `main`)
- `cdx/gate-3-protocol-lifecycle` (merged to `main`)
- `cdx/gate-4-workflow-planning` (merged to `main`)
- `cdx/gate-4-workflow` (merged to `main`)
- `cdx/gate-5-provenance` (merged to `main`)
- `cdx/gate-9-shared-identity` (merged to `main`)
- `cdx/two-model-codex-workflow` (historical merged workflow setup branch)
- `cdx/shared-identity-adr-0007` (reconnaissance planning branch)

## Current Queue

- `main` includes Gate 9 at `efde929b142256b6b29906924377eb6607734d6c`.
- Gate 9 merge-candidate CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28273143941`.
- Next primary branch: `cdx/gate-6-bundle-planning`.
- Gate 6 implementation is blocked until bundle/artifact and snapshot equality decisions are frozen.

## Cleanup Candidates

- `cdx/gate-9-shared-identity`
- `cdx/gate-5-provenance`
- `cdx/two-model-codex-workflow`
- `cdx/main-gate2-merge`
- `cdx/gate-4-workflow`
- `cdx/gate-4-workflow-planning`
- `cdx/gate-3-protocol-lifecycle`
- `cdx/gate-3-planning-decisions`
- `cdx/gate-2-digest-kernel-cleanup`
- `cdx/run-gate-zero-discovery`

## Not Safe To Delete

- `main`
- `cdx/run-gate-0-discovery`

## Verification

- `git branch --merged main` currently confirms the above cleanup candidates.
