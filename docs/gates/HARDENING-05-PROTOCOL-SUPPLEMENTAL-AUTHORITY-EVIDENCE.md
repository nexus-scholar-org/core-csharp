# Hardening 05 Evidence: Protocol Supplemental Authority

Status: complete locally

## Behavior Implemented

- Added target-specific supplemental approval records for Protocol waivers and amendments.
- Rehydration recomputes the approval-record digest and resolves a human actor, exact target, target digest, policy identity, policy version, policy mode, decision, roles, actor distinctness, and exact approval membership.
- Waiver rehydration verifies canonical waiver content and its resolved policy before returning immutable verified authority.
- Amendment rehydration resolves exact previous and produced verified Protocol versions, validates supersession and amendment lineage, and requires one immutable invalidation notice for every changed decision key.
- Public input types remain unverified claims. Verified wrappers have no public constructor.

## Invariants Enforced

- Automation cannot supply supplemental scientific authority.
- Rejected, withdrawn, missing, extra, duplicate, wrong-target, wrong-policy, wrong-role, stale-digest, or non-human approvals cannot promote a claim.
- A waiver or amendment cannot select a policy different from resolved authority.
- Amendment authority cannot cross Protocol identities, versions, content digests, amendment identities, or supersession links.
- Foreign, duplicate, missing, malformed, or replacement invalidation-notice membership is rejected.
- Verified wrappers copy caller collections into read-only storage.

## Tests And Recipes

- Protocol focused tests: 64 passed.
- Protocol conformance focused tests: 10 passed.
- Architecture tests: 25 passed.
- Full solution: 485 passed, 0 failed, 0 skipped.
- Six deterministic Hardening 05 recipes cover valid waiver/amendment replay plus wrong target, non-human actor, wrong lineage, and foreign notice cases.
- Existing fixture files were not regenerated or edited.

## Commands Run

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

All commands passed. The solution build reported zero warnings and zero errors. The repository verifier also passed the doctor and deterministic no-network local demo.

## Scientific-Invariant Review

No blocking, important, or minor findings remain. The final diff preserves human authority, exact provenance-bearing target binding, deterministic canonical digests, immutable approval and notice membership, complete amendment lineage, and fail-closed downstream invalidation.

## Files Changed

- `docs/adr/0019-protocol-supplemental-authority-records.md`
- `docs/gates/HARDENING-05-PROTOCOL-SUPPLEMENTAL-AUTHORITY.md`
- `docs/gates/HARDENING-05-PROTOCOL-SUPPLEMENTAL-AUTHORITY-EVIDENCE.md`
- `docs/reviews/2026-07-11-hardening-plan/README.md`
- `src/NexusScholar.Protocol/ProtocolModels.cs`
- `src/NexusScholar.Protocol/ProtocolSupplementalAuthority.cs`
- `tests/NexusScholar.Core.Tests/ProtocolTests.cs`
- `tests/NexusScholar.Conformance.Tests/ProtocolFixtureTests.cs`
- six new files under `fixtures/conformance/protocol/`

## Remaining Risk And Next Dependency

Workflow waiver/amendment compilation remains intentionally fail-closed. The next gate must consume `VerifiedProtocolWaiver` and `VerifiedProtocolAmendment` without accepting public waiver/amendment claims or approval ID strings.

## ADR Impact

ADR 0019 records the new supplemental authority contract and is implemented by this gate. No existing accepted ADR was superseded.

## Compatibility Impact

No PHP or blueprint compatibility claim is made. The new recipes are local hardening contracts only.
