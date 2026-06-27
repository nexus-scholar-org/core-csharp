# Codex Branch Board

Source: post-merge branch probes from local `main` after Gate 9 merge.

## Main Baseline

- Gate 9 merge commit: `efde929b142256b6b29906924377eb6607734d6c` (`docs: record gate 9 hosted ci evidence`).
- Gate 0 through Gate 5 and Gate 9 are merged into `main`.
- Gate 9 merge-candidate hosted CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28273143941`.
- Gate 9 branch `cdx/gate-9-shared-identity` is included in the merge baseline.

## Branch Classes

- merged: `main`, `cdx/gate-9-shared-identity`, `cdx/gate-5-provenance`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/shared-identity-adr-0007`, `cdx/run-gate-zero-discovery`, `cdx/run-gate-0-discovery`
- cleanup: `cdx/gate-9-shared-identity`, `cdx/gate-5-provenance`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/run-gate-zero-discovery`
- active: `main`
- blocked: Gate 6 implementation is blocked until `CF-002` and `CF-014` decisions are planned.
- stale: `cdx/run-gate-0-discovery`, `cdx/main-gate2-merge`
- review: none identified by current git state

Cleanup candidates above are confirmed by `git branch --merged main`.

## Safe Cleanup Candidates

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

## Next Work

- Next active branch target is `cdx/gate-6-bundle-planning`.
- Gate 6 must plan the bundle/artifact contract before implementation because `CF-002` remains blocking and `CF-014` affects snapshot and bundle equality.
- Gate 6 must not claim blueprint conformance, PHP compatibility, provenance parity, AI governance parity, persistence, API, UI, cloud sync, or workflow execution.

## Unresolved Ambiguity

- `cdx/run-gate-0-discovery` is still retained for historical reference.
