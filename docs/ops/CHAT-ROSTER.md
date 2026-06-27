# Chat Roster

Branch-derived Codex lane roster from current git state after the Gate 5 provenance merge.

## Active Lanes

- Lane `main`: merged baseline containing Gate 0 through Gate 5, Gate 5 merge commit `360ed8b`.
- Lane `gate-5-provenance`: merged closeout branch `cdx/gate-5-provenance`, head `360ed8b`.
- Lane `gate-4-workflow`: merged implementation branch `cdx/gate-4-workflow`, head `9ccc795`.
- Lane `gate-4-workflow-planning`: merged planning branch `cdx/gate-4-workflow-planning`, head `3cf28ce`.
- Lane `gate-3-protocol-lifecycle`: merged closeout branch `cdx/gate-3-protocol-lifecycle`, head `b513d6a`.
- Lane `gate-3-planning-decisions`: merged planning branch `cdx/gate-3-planning-decisions`, head `d925796`.
- Lane `gate-2-digest-kernel-cleanup`: merged kernel cleanup branch `cdx/gate-2-digest-kernel-cleanup`, head `5e5dde1`.
- Lane `gate-2`: merged evidence branch `cdx/run-gate-zero-discovery`, head `e17ec4f`.
- Lane `gate-0`: stale bootstrap branch `cdx/run-gate-0-discovery`, head `ee46eb4`.

## Branch Containment Relationships

- `main` contains Gate 0 through Gate 5.
- `main` contains the two-model workflow setup branch and the Gate 9 shared-identity ADR/reconnaissance branch.
- `cdx/gate-5-provenance` is now a merged historical lane rather than an active delivery branch.

## Status Notes

- Gate 5 local provenance ledger scope is merged.
- Gate 6 bundle/artifact contract planning is the next sequential gate and remains blocked on `CF-002`; snapshot equality questions under `CF-014` also need attention before claiming bundle/snapshot parity.
- Gate 9 shared identity implementation can continue as optional parallel local C# work, without PHP compatibility, Search, Deduplication, Screening, or snapshot semantics claims.
- Cleanup-safe merged lanes now include `cdx/gate-5-provenance`, `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-4-workflow`, `cdx/gate-4-workflow-planning`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, and `cdx/run-gate-zero-discovery`.
