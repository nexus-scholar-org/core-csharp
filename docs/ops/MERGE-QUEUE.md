# Merge Queue

Source: live status after public-feedback onboarding, `gh-pages` walkthrough, PR #6 UI presentation merge, and stale branch cleanup.

## Current Queue

Active branch:

- `cdx/state-refresh-public-comprehension`

Purpose:

- docs-only state refresh so ops/review files describe the current `main` and `gh-pages` state.

Current baseline:

- `main` head: `ac0307c` (`Polish Avalonia sample host presentation and scrolling`).
- `origin/main`: `ac0307c`.
- `gh-pages` / `origin/gh-pages`: `32475f4` (`docs(site): add first tester getting started walkthrough`).
- Hosted `main` CI: `gate-01` run `28402404840`, passed on Ubuntu and Windows for `ac0307c`.
- Remote branches: `main`, `gh-pages`.
- Local durable branches: `main`, `gh-pages`.

## Completed Consolidation

- `origin/main` was advanced from `16cabc3` through Full Text implementation, review refresh, public-feedback onboarding, local CLI demo, and UI presentation polish.
- Full Text implementation was ported from old local commit `a520616` into `main` as `5a13abc`.
- Review and README refresh landed as `ebb7bba`.
- Review/ops state refresh landed as `e79f5cd`.
- First public-feedback plan landed as `7cd63ae`.
- Public feedback onboarding and local CLI demo landed as `506ab35`.
- Avalonia sample host presentation and scrolling polish landed as `ac0307c`.
- `gh-pages` first-tester tutorial landed as `32475f4`.
- Remote and local `cdx/*` branches from the completed packets were deleted.

## Completed Merges And Ports

- Gate 0 through Gate 6 local foundations.
- Gate 9 Shared Identity.
- Gate 9 Search reconnaissance, contract, and local Search.
- Gate 9 Search Import contract and local import.
- Gate 9 Deduplication reconnaissance, contract, and local Deduplication.
- Gate 9 Screening reconnaissance, contract, and local Screening.
- Gate 9 Full Text reconnaissance and ADR 0014 contract.
- Gate 9 local no-network Full Text implementation.
- UI contract/block-plan samples.
- Avalonia block renderer prototype.
- Avalonia sample host.
- Public-feedback issue templates and PR template.
- CLI `doctor`, `sample`, and deterministic local `demo`.
- Public getting-started walkthrough on `gh-pages`.

## Not Queued Yet

- live provider/network calls
- Unpaywall, PMC, Europe PMC, arXiv, OpenAlex, Semantic Scholar, or publisher adapters
- Scopus API
- Web of Science API
- Google Scholar scraping
- paywall bypass or shadow-library sources
- actual PDF text extraction
- OCR
- full-text artifact storage
- PHP compatibility claims
- generated PHP fixtures
- persistence/API/cloud behavior
- product desktop shell behavior
- AI governance beyond current proposal contracts

## Active Work

Current packet:

- State-refresh docs after public-feedback and UI presentation merges.

Allowed scope:

- ops docs;
- review docs;
- maintainer routing docs if they still point at old work.

Do not implement AppServices, providers, persistence, API/cloud, PDF/OCR, live HTTP, or a UI product shell in this branch.

## Next Queue

Recommended next packets after this docs refresh:

1. **Public tester polish**: sample-host screenshot/GIF, root `LICENSE` / `CONTRIBUTING.md` / `SECURITY.md`, README links to public tutorial and issue templates.
2. **CLI public-path CI smoke**: run `doctor`, `sample`, and `demo` in CI or `scripts/verify`.
3. **APP-01 planning**: draft ADR 0015 for read-only AppServices composition from Search Import + Deduplication into `WorkspacePlan`.
4. **Provider planning note only**: provider/network/legal questions without implementation.

## Verification

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

Current source scan:

- 324 `[TestMethod]` declarations.

Latest hosted verification:

- `gate-01` run `28402404840`
- commit `ac0307c`
- result: passed on `ubuntu-latest` and `windows-latest`
- URL: https://github.com/nexus-scholar/core-csharp/actions/runs/28402404840
