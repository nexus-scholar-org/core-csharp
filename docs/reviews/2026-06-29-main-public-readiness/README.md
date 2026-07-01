# Main Public-Readiness Review - 2026-06-29

Status: refreshed after public-feedback onboarding, deterministic CLI demo, `gh-pages` first-tester walkthrough, PR #6 UI presentation merge, branch cleanup, and hosted CI.

Current implementation baseline: `origin/main` at `ac0307c` (`Polish Avalonia sample host presentation and scrolling`).

Public site branch: `origin/gh-pages` at `32475f4` (`docs(site): add first tester getting started walkthrough`).

Remote branch state:

- `origin/main`
- `origin/gh-pages`

The older reports in this folder remain useful historical audit records. The current state is this README plus `06-state-refresh-after-public-feedback-and-ui-polish.md`.

## Reports

- `01-main-baseline-audit.md` - historical baseline audit from `ebb7bba`; superseded for current branch state by report 06.
- `02-branch-cleanup-plan.md` - historical branch cleanup record from the earlier consolidation pass.
- `03-public-readiness-and-feedback-plan.md` - current public-readiness and feedback plan after PF/CLI/WEB/UI progress.
- `04-continuation-roadmap.md` - current practical next path after public-feedback onboarding.
- `05-state-refresh-after-remote-cleanup.md` - historical post-cleanup state snapshot from `ebb7bba`.
- `06-state-refresh-after-public-feedback-and-ui-polish.md` - current state after public-feedback onboarding, first-tester walkthrough, and UI polish.

## Current Verdict

Nexus Scholar Core is no longer just a scaffold. `main` contains a real local-first kernel, architecture guards, conformance fixtures, local no-network Full Text evidence, public feedback scaffolding, a deterministic CLI `demo`, UI contracts, an Avalonia renderer, and a polished sample host.

The project is still not a researcher-usable systematic-review product. The honest public posture is:

> Nexus Scholar Core is a verified research workflow kernel and public architecture foundation, with a deterministic local CLI demo and sample-only UI inspection host. It is ready for architecture feedback, developer critique, methodology critique, and carefully framed first-tester conversations, not for real review execution by non-developers.

## Current Implementation Surface

- deterministic kernel;
- protocol lifecycle;
- workflow compiler;
- artifact identity;
- provenance ledger;
- bundle verifier;
- shared scholarly identity;
- Search trace and local search-import behavior;
- Deduplication;
- Screening;
- local no-network Full Text;
- plugin capability contracts;
- governed AI proposal contracts;
- UiContracts;
- Avalonia block renderer;
- Avalonia sample host with scrolling/presentation polish;
- CLI `doctor`, `sample`, and deterministic local `demo`;
- public-feedback issue templates and PR template;
- public first-tester walkthrough on `gh-pages`.

## Current Measurements

Measured from the current worktree:

- 16 source projects under `src/`.
- 2 sample folders under `samples/`.
- 7 test projects under `tests/`.
- 146 C# files under `src/`.
- 71 C# files under `tests/`.
- 168 fixture files under `fixtures/`.
- 14 ADR files.
- 324 `[TestMethod]` declarations.

## Current Public Gaps

- Add a fresh sample-host screenshot or GIF to the `gh-pages` first-tester walkthrough.
- Add root `LICENSE`, `CONTRIBUTING.md`, and `SECURITY.md`.
- Add CI or script smoke for the public CLI path: `doctor`, `sample`, and `demo`.
- Plan APP-01 as a narrow read-only AppServices composition ADR before code.
- Keep provider work planning-only until provider/network/legal boundaries are accepted.

## Current Validation

Latest hosted verification for current `main`:

```text
gate-01 run 28402404840
Commit: ac0307c
Result: passed on ubuntu-latest and windows-latest
URL: https://github.com/nexus-scholar/core-csharp/actions/runs/28402404840
```

Latest known local post-merge verification for `main`:

```text
dotnet build NexusScholar.Core.slnx -c Release
dotnet test NexusScholar.Core.slnx -c Release --no-build
dotnet run --project samples/NexusScholar.Avalonia.Blocks.SampleHost
```

Result:

- build passed;
- tests passed;
- sample host launch smoke passed and closed cleanly.
