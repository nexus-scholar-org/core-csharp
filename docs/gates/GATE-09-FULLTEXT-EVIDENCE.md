# Gate 9 Full Text Evidence

Status: local verification completed for the first no-network Full Text implementation slice.

## Branch

- `cdx/gate-9-fulltext-local`
- Base used: `f6667b0` (`docs: refresh ops after full text contract merge`)

`origin/main` had advanced to include the Avalonia renderer merge, so this branch was created from the accepted Full Text base to avoid mixing UI-lane changes into the Full Text work.

## Behavior Implemented

- Full Text input accepts Screening include and allowed needs-review handoff.
- Raw Search traces and raw Dedup member records are rejected as Full Text input.
- Final exclude decisions are not retrieval candidates by default.
- Acquisition records require actor and timestamp for user-supplied/manual acquisition.
- Source attempts preserve failed/skipped attempts when a later success exists.
- Raw artifact evidence uses exact bytes plus `raw-artifact-bytes`.
- PDF/XML/text validators classify stable ADR categories.
- Local paths, routes, app ids, CLI manifests, and PHP rows are projections only.
- Duplicate artifact detection uses raw byte digest only and does not merge candidates.
- Derived extraction records bind to source artifact id, source raw digest, and digest scope.

## Local Verification

Commands run during implementation:

```powershell
dotnet build NexusScholar.Core.slnx -c Release
dotnet test NexusScholar.Core.slnx -c Release --no-build
```

Observed result before final formatting pass:

- build: passed, 0 warnings, 0 errors
- tests: passed, 306 total
  - `NexusScholar.UiContracts.Tests`: 18 passed
  - `NexusScholar.Architecture.Tests`: 17 passed
  - `NexusScholar.Core.Tests`: 186 passed
  - `NexusScholar.Conformance.Tests`: 85 passed

Final verification commands and results:

- `dotnet restore NexusScholar.Core.slnx`: passed
- `dotnet build NexusScholar.Core.slnx -c Release --no-restore`: passed, 0 warnings, 0 errors
- `dotnet test NexusScholar.Core.slnx -c Release --no-build`: passed, 306 total
- `dotnet format NexusScholar.Core.slnx --verify-no-changes --no-restore`: passed
- `powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1`: passed, 0 warnings, 0 errors, 306 tests passed

## Invariants Enforced

- Suggestion is not a decision.
- Raw artifact bytes are artifact identity.
- Extracted text is derived evidence and never replaces raw artifact evidence.
- Failed and skipped source attempts remain audit evidence.
- Duplicate artifacts do not merge works, candidates, clusters, or Screening decisions.
- Paths, routes, storage ids, app row ids, CLI manifests, and PHP `pdf_fetches` are projections.
- Provider/network/legal behavior remains blocked.

## Conflict Status

- `CF-025`: implemented/resolved for local Full Text artifact evidence.
- `CF-026`: narrowed; live provider/network/legal behavior remains blocked.
- `CF-027`: narrowed; app projection boundary remains preserved.
- `CF-024`: unchanged.

## Explicit Non-Claims

- no live provider/network behavior
- no HTTP clients
- no provider SDKs or credentials
- no scraping, paywall bypass, or shadow-library behavior
- no actual PDF parsing
- no OCR
- no persistence/API/UI/cloud behavior
- no CLI/Web behavior changes
- no PHP reference changes
- no generated PHP fixtures
- no PHP compatibility claim
