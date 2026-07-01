# Start Here

Launch Codex from this directory and read `AGENTS.md` first.

This repository is no longer at Gate 0 discovery. The current baseline is `main` after public-feedback onboarding, deterministic CLI demo, `gh-pages` first-tester walkthrough, and Avalonia sample-host presentation polish.

## Current Routing

1. Read `README.md` for the current implementation surface and non-claims.
2. Read `docs/ops/BRANCH-BOARD.md` and `docs/ops/MERGE-QUEUE.md` for live branch state and recommended next work.
3. Read `docs/reviews/2026-06-29-main-public-readiness/README.md` for the current public-readiness state.
4. Use `docs/ops/FIRST-PUBLIC-FEEDBACK-PLAN-2026-06-29.md` as historical plan and task trace, not as a command to restart completed PF/CLI/WEB work.
5. For implementation work, read the relevant accepted ADRs in `docs/adr/` and the target gate/evidence docs in `docs/gates/`.
6. Preserve the non-claims in `README.md`, `AGENTS.md`, and the public-feedback plan.

## Current Next Work

Recommended order:

1. finish stale-state refresh if any ops/review docs still cite old branch state as current;
2. finish first-tester polish: sample-host screenshot/GIF, root public contributor docs, and feedback routing links;
3. add public CLI smoke to CI or `scripts/verify`;
4. plan APP-01 as a read-only AppServices composition ADR before code.

## Verification

For normal changes run:

```powershell
dotnet build NexusScholar.Core.slnx -c Release
dotnet test NexusScholar.Core.slnx -c Release --no-build
dotnet format NexusScholar.Core.slnx --verify-no-changes --no-restore
```

The repository script is also available:

```powershell
powershell -ExecutionPolicy Bypass -File ./scripts/verify.ps1
```

Do not add live providers, persistence, API/cloud behavior, PDF/OCR, provider SDKs, or UI product-shell behavior unless a later ADR and task explicitly authorize it.
