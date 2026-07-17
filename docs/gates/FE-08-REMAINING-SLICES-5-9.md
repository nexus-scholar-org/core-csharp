# FE-08 Remaining Slices 5-9: Local Desktop Review Continuation

Status: accepted and gate-complete on 2026-07-17.
Source of truth: ADR 0038 (in-scope contract remains governed by this hardening stream).

Scope:
1. Slice 6 — correction/adjudication/handoff provenance and post-handoff closure.
2. Slice 7 — per-candidate local Full Text intake/review with persisted criteria.
3. Slice 8 — deterministic protocol-bound reporting workflow authority, final report, Bundle v2 exact inventory, export ledger and round-trip verification.
4. Slice 9 — desktop preview/confirm, accessibility labels, visual check, architecture/recovery gates.

## Implementation posture

- This file is the accepted remaining-slices contract and evidence index.
- Slices 6-9 are the only scope additions in this update; Slice 5 remains accepted from the preceding commit.
- Slices 1-4 are already documented and remain unchanged:
  - `FE-08-LOCAL-PRODUCT-DESKTOP-SLICES-1-2.md`
  - `FE-08-DESKTOP-DEDUPLICATION-REVIEW-SLICE-3.md`
  - `FE-08-SCREENING-REVIEW-SLICE-4` equivalent acceptance artifacts.
- Full-solution gates and independent reviews passed; this is not a production-readiness or go-live certification.

## Slice 6 — correction/adjudication/handoff closure (implemented set)

- Corrections and adjudications must be previewed with exact authority/material reconstruction before confirm.
- Exact superseded-decision lineage, actor-role binding, workflow snapshot context, and invalidation references are required in tokens/results.
- Handoff objects must preserve closure provenance (including rationale, chain, and terminal state) and refuse partial handoff execution.
- Any authority drift, actor/role mismatch, or target revision change between preview and confirm results in stale or recovery-required outcomes.

## Slice 7 — local Full Text intake/review (implemented set)

- Full Text intake is local-only and candidate-local; remote URL fetch is not accepted for local intake.
- Candidate review uses per-candidate persisted criteria and local artifact evidence (bytes + digest + parser status + extraction attempt trace).
- Every Full Text screening action is previewed and confirmed with strict stale checks.
- A Full Text decision cannot be confirmed without verified handoff preconditions and include/exclude intent.

## Slice 8 — deterministic protocol-bound reporting, Bundle v2, export ledger, round-trip verification (implemented set)

- Final report is authority-driven and depends on deterministic source digests, finalized report slice, and terminal review completeness.
- Bundle v2 generation is inventory-exact and must enforce exact path/canonical entry checks.
- Export publication must append deterministic ledger entries with replay/provenance metadata.
- Round-trip verification should reconstruct from canonical records only (no display or UI state as authority).

## Slice 9 — closeout: preview/confirm, accessibility labels, visual and architecture recovery gates (implemented set)

- Desktop confirmation/preview flows remain non-authoritative UI surfaces.
- Recovery requirements: failed publish/export/verify cannot mutate project revision or ledger state.
- Accessibility labels and navigation state for critical review and confirmation actions must remain explicit and deterministic.
- Architecture requirement remains active: no UI-framework reference in Core domain packages.

## Exact invariants for slices 5-9

- Human actor identity and role must be in scope for every mutating operation.
- Confirmation token must include stable fields for project revision, authority generation, manifest pointer, decision-set/snapshot, criteria digest, actor/role, preview digest, and exact superseded reference.
- Both preview and confirm must rehydrate and compare live authority/package state.
- Operational outcomes must remain distinct: success, validation failure, stale, recovery required, authority unavailable.
- Science state updates are append-only with explicit invalidation lineage.

## Explicit nonclaims

- No claim of production readiness, deployment certification, or external compliance completion.
- No claim for non-local Full Text acquisition behavior.
- No claim on network, provider, or PHP-level parity for these slices.

## PHP impact

- PHP impact: none; no compatibility claim or PHP fixture change.

## Exclusions

- No provider/network scraping, no cloud sync, no authentication stack expansion, and no new core-domain production dependencies.
- No full UI redesign scope; this remains workflow/authority documentation and closeout planning.

## Completion evidence

- Focused negative tests cover stale actor/project revision, supersession mismatch, remote-intake rejection, per-candidate Full Text isolation, Bundle/ledger integrity violations, and recovery mutation defects.
- Aggregate build, 924-test solution run, format, architecture, conformance, and 23-package deterministic smoke passed on 2026-07-17.
- Visual and accessibility inspection completed at 1360x900 with explicit automation names on critical review controls.
- Exact commands and review disposition are recorded in `FE-08-REMAINING-SLICES-5-9-EVIDENCE.md`.
