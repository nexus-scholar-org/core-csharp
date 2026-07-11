# Hardening 05: Protocol supplemental authority

Status: accepted for implementation

## Goal

Provide verified Protocol waiver and amendment authority records so downstream Workflow compilation can resolve human approvals, complete version lineage, and immutable invalidation-notice membership instead of trusting approval ID strings.

## Sources

1. `AGENTS.md`
2. `docs/adr/0002-canonical-json-and-digests.md`
3. `docs/adr/0003-protocol-record-contract.md`
4. `docs/adr/0004-protocol-approval-semantics.md`
5. `docs/adr/0018-workflow-authority-hardening-sequence.md`
6. `docs/adr/0019-protocol-supplemental-authority-records.md`
7. `docs/reviews/2026-07-11-hardening-plan/full-technical-review.md`

## Dependency-Ordered Tasks

1. Protocol owner: define unverified and verified target-specific supplemental approval records.
2. Protocol owner: rebuild supplemental approval canonical material and verify its approval-record digest against resolved human actor and policy authority.
3. Protocol owner: define unverified waiver/amendment claims and verified wrappers.
4. Protocol owner: verify waiver canonical content, target digest, policy, roles, actors, distinctness, and exact approval membership.
5. Protocol owner: verify amendment canonical content, target digest, previous/produced verified versions, supersession, policy, and exact approval membership.
6. Protocol owner: verify unique immutable invalidation-notice membership and reject replacement, foreign, duplicate, or malformed notices.
7. Test/fixture owner: add focused regressions and deterministic replay recipes for all positive and negative authority cases.
8. Gate owner: run focused, full, architecture, conformance, formatting, repository, scientific-invariant, and hosted CI verification.

## Required Cases

- valid waiver authority resolves exact human approvals and reproduces waiver and approval digests;
- valid amendment authority resolves exact previous/produced versions and immutable notice membership;
- missing, extra, duplicate, rejected, withdrawn, non-human, wrong-role, wrong-policy, wrong-target, or stale-digest approvals are rejected;
- wrong protocol, previous version, produced version, previous digest, amendment ID, supersession link, or requested actor is rejected;
- missing, duplicate, foreign, replaced, or malformed invalidation notices are rejected;
- verified outputs do not retain mutable caller collections;
- public unverified records cannot instantiate verified authority wrappers.

## Allowed Paths

- `docs/adr/0019-protocol-supplemental-authority-records.md`
- `docs/gates/HARDENING-05-PROTOCOL-SUPPLEMENTAL-AUTHORITY.md`
- `docs/gates/HARDENING-05-PROTOCOL-SUPPLEMENTAL-AUTHORITY-EVIDENCE.md`
- `docs/reviews/2026-07-11-hardening-plan/README.md`
- `src/NexusScholar.Protocol/ProtocolModels.cs`
- `src/NexusScholar.Protocol/ProtocolSupplementalAuthority.cs`
- `tests/NexusScholar.Core.Tests/ProtocolTests.cs`
- `tests/NexusScholar.Conformance.Tests/ProtocolFixtureTests.cs`
- `fixtures/conformance/protocol/`

## Excluded Paths

- restoring Workflow waiver/amendment compilation in this gate
- deviation approval transitions
- persistence, API, CLI, UI, provider, plugin, AI, artifact storage, or workspace behavior
- existing fixture regeneration
- PHP or blueprint compatibility claims
- production dependencies

## Risks And Decisions

- Approval IDs are part of current waiver/amendment content. The supplemental approval target digest therefore binds the final record containing those predetermined approval IDs.
- Resolvers are application-owned trust boundaries and must return durable actor, policy, approval, and Protocol-version records.
- Invalidation notices are verified through exact amendment membership; no independent replacement list is accepted.
- No source conflict remains after ADR 0019.

## Verification

```powershell
dotnet restore NexusScholar.Core.slnx
dotnet build NexusScholar.Core.slnx -c Release --no-restore
dotnet test tests/NexusScholar.Core.Tests/NexusScholar.Core.Tests.csproj -c Release --no-build --filter ProtocolTests
dotnet test tests/NexusScholar.Conformance.Tests/NexusScholar.Conformance.Tests.csproj -c Release --no-build --filter ProtocolFixtureTests
dotnet test tests/NexusScholar.Architecture.Tests/NexusScholar.Architecture.Tests.csproj -c Release --no-build
dotnet test NexusScholar.Core.slnx -c Release --no-build
dotnet format NexusScholar.Core.slnx --verify-no-changes --no-restore
./scripts/verify.ps1
git diff --check
```

## Exit Checklist

- [x] Supplemental approval records are target-specific, human-resolved, and digest-verified.
- [x] Waiver authority requires exact policy and approval membership.
- [x] Amendment authority requires exact version lineage and notice membership.
- [x] Verified outputs are immutable and non-fabricable.
- [x] Every required case has a permanent test or recipe.
- [x] Existing fixtures remain unchanged.
- [x] Only allowed paths changed.
- [x] Focused and full verification pass.
- [x] Scientific-invariant review is clear.
- [x] Evidence records behavior, commands, totals, risks, ADR impact, and compatibility impact.
