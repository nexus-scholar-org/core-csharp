# Hardening 12: Shared Transitive Identity

Status: accepted and implemented locally

## Goal

Start Phase 3 by enforcing transitive stable-identifier closure in validated corpus slices.

## Invariants

- adding an identified work merges the complete overlap-connected component;
- no two members produced by validated operations share a stable identifier;
- the earliest existing overlapping member remains the metadata representative;
- bridge merging is safe across all input permutations;
- repeated additions do not recreate duplicate membership;
- `RehydrateValidated` rejects persisted slices with overlapping stable identity;
- unresolved no-ID candidates remain distinct and unchanged.

## Evidence

- focused unit tests cover bridge closure, all six three-record permutations, repeated additions, and rehydration rejection;
- a local conformance fixture records DOI/OpenAlex/S2 bridge closure;
- fixtures remain local contract evidence and make no PHP compatibility claim.

## Deferred

- representative metadata policy beyond the accepted earliest-member behavior remains a Deduplication concern;
- PHP compatibility remains deferred until generator-backed fixtures and semantic comparison exist.
