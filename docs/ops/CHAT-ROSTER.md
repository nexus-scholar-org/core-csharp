# Chat Roster

Branch-derived Codex lane roster from current git state.

## Active lanes
- Lane `gate-3-protocol-lifecycle`: current head `cdx/gate-3-protocol-lifecycle`, commit `a8b9f68`.
- Lane `gate-3-planning-decisions`: planning decisions branch `cdx/gate-3-planning-decisions`, commit `d925796`.
- Lane `gate-2-digest-kernel-cleanup`: kernel cleanup lane `cdx/gate-2-digest-kernel-cleanup`, commit `5e5dde1`.
- Lane `gate-2`: archived evidence branch `cdx/run-gate-zero-discovery`, commit `e17ec4f`.
- Lane `gate-0`: legacy bootstrap branch `cdx/run-gate-0-discovery`, commit `ee46eb4`.
- Lane `main`: base line `main`, commit `ee46eb4`.

## Branch containment relationships
- `gate-3-planning-decisions` (`d925796`) is already contained in `gate-3-protocol-lifecycle`.
- `gate-2-digest-kernel-cleanup` (`5e5dde1`) is contained in both `gate-3-planning-decisions` and `gate-3-protocol-lifecycle`.
- `run-gate-zero-discovery` (`e17ec4f`) is contained in `gate-2-digest-kernel-cleanup`, `gate-3-planning-decisions`, and `gate-3-protocol-lifecycle`.
- `gate-3-protocol-lifecycle` (`a8b9f68`) is the terminal historical head for this set.

## Merge readiness signals
- Ready: `cdx/gate-2-digest-kernel-cleanup` and `cdx/gate-3-planning-decisions` are already included in `cdx/gate-3-protocol-lifecycle`.
- Not yet merged to remote baseline: `cdx/gate-2-digest-kernel-cleanup`, `cdx/gate-3-planning-decisions`, and `cdx/gate-3-protocol-lifecycle` are not fully merged into `origin/main` by current ancestry check.

## Stale and cleanup notes
- Stale lane candidates: `cdx/run-gate-0-discovery` and `main` (same base commit age and no branch-local forward movement).
- Keep these lanes in docs if you need retrospective traceability.
- Cleanup candidates after archival: `cdx/run-gate-0-discovery`, optionally `main` upstream tracking, and older local-only branch tracking debt.
