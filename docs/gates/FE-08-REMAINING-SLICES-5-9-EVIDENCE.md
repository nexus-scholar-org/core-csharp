# FE-08 Remaining Slices 6-9 Completion Evidence

Status: accepted on 2026-07-17.

## Delivered scope

- Slice 6: human correction, adjudication, and handoff preview/confirm with exact lineage, closure provenance, stale checks, and post-handoff mutation rejection.
- Slice 7: local-only per-candidate Full Text intake and review with immutable raw bytes, extraction evidence, persisted protocol criteria, and independent replay.
- Slice 8: deterministic reporting Workflow authority, conserved final report, exact-inventory Bundle v2, human export request, append-only ledger, and round-trip replay.
- Slice 9: desktop projections and confirmation controls, explicit automation names, recovery-state propagation, visual inspection, and architecture enforcement.

Slice 5 was completed before this remaining-slices implementation and is not re-claimed here.

## Verification

Run from the repository root with SDK `10.0.301`:

```powershell
C:\Users\mouadh\.dotnet\dotnet.exe build NexusScholar.Core.slnx -c Release -m:2
C:\Users\mouadh\.dotnet\dotnet.exe test NexusScholar.Core.slnx -c Release --no-build -m:2
C:\Users\mouadh\.dotnet\dotnet.exe format NexusScholar.Core.slnx --verify-no-changes --no-restore
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-packages.ps1
```

Results:

- Release build: passed with 0 warnings and 0 errors.
- Full solution: 924 passed, 0 failed, 0 skipped.
- Architecture: 39 passed.
- Conformance: 141 passed, including unchanged FE-06 reporting fixture digests.
- CLI: 79 passed.
- Core: 519 passed.
- Desktop AppServices: 36 passed.
- Desktop host: 11 passed.
- Format verification: passed.
- Package verification: 23 approved packages at `0.1.0-alpha.2`; normalized repeat pack and clean local-source smoke passed.
- `git diff --check`: clean.

## Negative and recovery evidence

- Desktop confirmation tokens bind and retain the exact ResearchWorkspace operation token.
- Candidate-specific Full Text review remains bound to the selected candidate even when another candidate is current.
- Full Text artifacts must resolve beneath their immutable generation root.
- Legacy Screening handoff v1 bytes retain their established digest; publication-evidence records round-trip without rewriting old fixtures.
- Export actor id and role are canonical request material.
- Export stale, validation, and recovery-required outcomes remain distinct.
- An injected pre-publication export fault leaves project revision and ledger head unchanged.
- Multi-candidate finalization asserts title/abstract, Full Text, and included-count conservation.
- Corpus-snapshot export provenance binds the verified Screening authority package generation and manifest, not the Deduplication generation pointer.
- Bundle v2 and export ledger replay reject altered, missing, extra, stale, or misbound material.

## Desktop inspection

- Native desktop host inspected at 1360x900.
- Three-column layout remained visible without overlap.
- Workspace, project, initialization, inspector, and command controls were exposed in the accessibility tree.
- Critical Full Text, reporting, and export inputs/actions now receive explicit `AutomationProperties.Name` values.

## Independent review

Three read-only reviewers inspected scientific authority, architecture, and test coverage. Their blocking/high findings were corrected and re-reviewed:

- per-candidate Desktop Full Text routing;
- corpus-snapshot source provenance;
- operation-token preservation and exact preview reconstruction;
- export actor-role binding and stale/recovery classification;
- Full Text generation-root path enforcement;
- report conservation, recovery, and accessibility evidence.

Final disposition: no blocking or high-severity findings remain in the accepted scope.

## Nonclaims

- No production-readiness, deployment, external compliance, or PRISMA certification claim.
- No network/provider Full Text acquisition claim.
- No PHP compatibility expansion.
- No change to scientific authority: desktop UI remains a non-authoritative projection and command surface.
