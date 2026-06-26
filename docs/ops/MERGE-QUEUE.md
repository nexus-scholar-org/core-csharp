# Merge Queue

Source: branch containment and ancestry observed from the requested git commands.

## Recommended queue
1. `cdx/run-gate-zero-discovery`
2. `cdx/gate-2-digest-kernel-cleanup`
3. `cdx/gate-3-planning-decisions`
4. `cdx/gate-3-protocol-lifecycle`
5. `origin/main`

## Queue rationale from ancestry
- `cdx/run-gate-zero-discovery` is the gate 2 evidence base for later branches.
- `cdx/gate-2-digest-kernel-cleanup` contains `e17ec4f` and is the next deterministic kernel improvement step.
- `cdx/gate-3-planning-decisions` contains `d925796` and bridges gate 2 work into protocol planning.
- `cdx/gate-3-protocol-lifecycle` contains `a8b9f68` and is the current Gate 3 implementation line.
- `origin/main` remains at `8e1e252` and does not include the new Gate 2/3 branches by the current check.

## Containment gates to verify
- `d925796` must stay contained in the Gate 3 line.
- `5e5dde1` must stay contained in any final protocol lifecycle merge target.
- `e17ec4f` should remain available through gate3 lifecycle until protocol evidence lock.
- `a8b9f68` is the protocol-lifecycle head and should be treated as the Gate 3 merge head.

## Cleanup candidates tied to queue
- `cdx/gate-3-planning-decisions` and `cdx/gate-2-digest-kernel-cleanup` can be retained as history notes until release tagging, then retired.
- `cdx/run-gate-zero-discovery` becomes removable after successful archival of hosted CI evidence if no new amendments are expected.
