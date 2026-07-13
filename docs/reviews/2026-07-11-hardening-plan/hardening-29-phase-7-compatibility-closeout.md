# Hardening 29 - Phase 7 Compatibility Closeout (Citation/Exports Evidence)

## Scope

H29 closes the remaining Phase 7 compatibility evidence work for citation-network and dissemination-export behavior by generating evidence-only PHP fixtures and recording a compatibility claim inventory for non-adoption and no broad compatibility claims.

## Behavior and evidence

- Fixture set generated: `fixtures/php-golden/citation-export/v1/`
- Source pin: pinned PHP commit in `specs/SOURCE.lock.json` (shared across phase-7 evidence runs)
- Case count: 14 total
- Classification: all intentional differences (`intentional_change`)
- ADR basis: `ADR 0027`
- C# semantic replay target: none (no Network/Reporting implementation exists in this cycle)
- Closure comparator: `tests/NexusScholar.Conformance.Tests/PhpCompatibilityEvidenceClosureTests.cs`
- Evidence-only intent: no `NexusScholar.Network` / `NexusScholar.Reporting` implementation changes are introduced for H29

## Invariants enforced

- The fixture set includes immutable input/output fingerprints and manifest data for replayability.
- Fixture generation remains scoped to evidence generation and does not alter existing C# logic.
- Retained compatibility statements in H29 explicitly name `fixtures/php-golden/citation-export/v1/` and the result class (`intentional_change`).
- Broad PHP compatibility claims are not asserted for H29; only scoped evidence records are recorded.

## Tests to run

- Comparator coverage:
  - `tests/NexusScholar.Conformance.Tests/PhpCompatibilityEvidenceClosureTests.cs`
  - H29-specific scenario list in `docs/port/PHASE-7-COMPATIBILITY-CLAIM-INVENTORY.md`
- Phase-7 verification checklist remains:
  - fixture set present and complete
  - manifest digest entries present
  - claim inventory aligned to sectioned classifications

## Non-claims

- No Network product compatibility claim.
- No Reporting (dissemination export) implementation claim.
- No C# semantic replay claim for citation or export behavior.
- No PHP-derived compatibility claim outside the explicit H29 evidence scope.

## Risks

- No comparison can validate C# parity where C# implementation is intentionally absent.
- Future implementers may treat exported graph/bibliography evidence as normative Core behavior without ADR mediation.
- Additional ADR or gate decisions may be required to expand from evidence-only to implementation/comparison scope.

## ADR / PHP impact

- ADR impact: H29 accepts `ADR 0027` as the normative basis for intentional non-adoption.
- PHP impact: H29 preserves evidence generated from pinned PHP behavior and does not alter fixture semantics or pin state.

## Pending

- Protected-branch merge verification remains pending for the H29 evidence and documentation closeout.
- Final merge readiness requires branch policy and release-gate review checks to approve the generated fixtures, comparator, ADR, and documentation.
