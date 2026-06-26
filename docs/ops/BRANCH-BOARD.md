# Codex Branch Board

Source: merge closeout state observed while preparing `main` for Gate 3.

## Merged To Main

- `cdx/run-gate-zero-discovery`
- `cdx/gate-2-digest-kernel-cleanup`
- `cdx/gate-3-planning-decisions`
- `cdx/gate-3-protocol-lifecycle`

## Current Gate State

- Gate 0: accepted as planning/evidence freeze.
- Gate 1: accepted.
- Gate 2: accepted for deterministic-kernel behavior.
- Gate 3: accepted for local protocol lifecycle behavior.

## Retain For History

- `cdx/run-gate-zero-discovery`
- `cdx/gate-2-digest-kernel-cleanup`
- `cdx/gate-3-planning-decisions`
- `cdx/gate-3-protocol-lifecycle`

These branches can remain as audit trails until a release tag or repository-maintenance pass retires them.

## Do Not Touch

- Uncommitted local `.codex` agent/config files in the primary workspace. They are operational environment changes, not gate evidence.

## Next Review Target

Gate 4 planning only: resolve `CF-003`, `CF-006`, and `CF-007` before workflow compiler implementation.
