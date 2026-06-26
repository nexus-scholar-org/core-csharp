# Chat Roster

Branch-derived Codex lane roster from current git state after the manual GitHub merge of the workflow branch.

## Active lanes

- Lane `main`: merged baseline `main`, commit `467d5f2` (`Merge pull request #1 from nexus-scholar/cdx/two-model-codex-workflow`).
- Lane `two-model-codex-workflow`: merged workflow branch `cdx/two-model-codex-workflow`, head `4ad8ba6`.
- Lane `gate-3-protocol-lifecycle`: merged closeout branch `cdx/gate-3-protocol-lifecycle`, head `b513d6a`.
- Lane `gate-3-planning-decisions`: merged planning branch `cdx/gate-3-planning-decisions`, head `d925796`.
- Lane `gate-2-digest-kernel-cleanup`: merged kernel cleanup branch `cdx/gate-2-digest-kernel-cleanup`, head `5e5dde1`.
- Lane `gate-2`: merged evidence branch `cdx/run-gate-zero-discovery`, head `e17ec4f`.
- Lane `gate-0`: stale bootstrap branch `cdx/run-gate-0-discovery`, head `ee46eb4`.

## Branch containment relationships

- `main` contains the workflow merge commit `467d5f2`.
- `main` also contains `0339d99` (`Merge Gate 3 protocol lifecycle`), which closes Gate 0 through Gate 3 into the baseline.
- `two-model-codex-workflow` is now a merged historical lane rather than an active delivery branch.

## Status notes

- Gate 4 is next planning work.
- Gate 4 implementation remains blocked by `CF-003`, `CF-006`, and `CF-007`.
- PHP Shared identity reconnaissance is allowed only as docs-only parallel work.
- Cleanup-safe merged lanes now include `cdx/two-model-codex-workflow`, `cdx/main-gate2-merge`, `cdx/gate-3-protocol-lifecycle`, `cdx/gate-3-planning-decisions`, `cdx/gate-2-digest-kernel-cleanup`, and `cdx/run-gate-zero-discovery`.
