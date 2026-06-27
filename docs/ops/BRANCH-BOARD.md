# Codex Branch Board

Source: post-merge branch probes from local `main` after Gate 5 merge.

## Main Baseline

- Gate 5 merge commit: `360ed8be5db1b41081255612da95fb27af11825b` (`docs: record gate 5 post-review evidence`).
- Gate 0 through Gate 5 are merged into `main`.
- Gate 5 merge-candidate hosted CI is green: `https://github.com/nexus-scholar/core-csharp/actions/runs/28272279336`.
- Gate 5 branch `cdx/gate-5-provenance` is included in the merge baseline.

## Branch Classes

- merged: `main`, `cdx/gate-5-provenance`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/shared-identity-adr-0007`, `cdx/run-gate-zero-discovery`, `cdx/run-gate-0-discovery`
- cleanup: `cdx/gate-5-provenance`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, `cdx/run-gate-zero-discovery`
- active: `main`
- blocked: none recorded
- stale: `cdx/run-gate-0-discovery`, `cdx/main-gate2-merge`
- review: none identified by current git state

Cleanup candidates above are confirmed by `git branch --merged main`.

## Safe Cleanup Candidates

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

- Next sequential gate planning target is Gate 6 bundle/artifact contract work, blocked until `CF-002` and related snapshot equality questions are resolved.
- Optional parallel branch: `cdx/gate-9-shared-identity` for local shared scientific identity implementation.
- PHP Shared Identity remains limited to reconnaissance-derived fixtures and comparator planning until a dedicated compatibility gate claims otherwise.

## Unresolved Ambiguity

- `cdx/run-gate-0-discovery` is still retained for historical reference.
