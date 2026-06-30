# State Refresh After Public Feedback And UI Polish

Date: 2026-07-01

## Current Git State

Current implementation baseline:

```text
origin/main ac0307c Polish Avalonia sample host presentation and scrolling
```

Current public site baseline:

```text
origin/gh-pages 32475f4 docs(site): add first tester getting started walkthrough
```

Current remote branches:

```text
origin/main
origin/gh-pages
```

Remote heads:

```text
32475f4e5fc2bf5b33becfff02d9607f16016fda refs/heads/gh-pages
ac0307c7a5e396c36325c42632a02b2faca10172 refs/heads/main
```

## What Is Now Complete

- PF-01 maintainer routing docs.
- PF-02 GitHub issue templates and PR template.
- CLI-01 local deterministic demo contract.
- CLI-02 local deterministic CLI `demo` command.
- CLI-03 README and public-readiness demo docs.
- WEB-01 public first-tester getting-started tutorial on `gh-pages`.
- UI presentation pass for the Avalonia sample host, including scrollable content and fixed status bar placement.
- Remote cleanup so only `main` and `gh-pages` remain as remote branches.

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

## Current Verification

Latest hosted verification:

```text
gate-01 run 28402404840
Commit: ac0307c
Result: passed on ubuntu-latest and windows-latest
URL: https://github.com/nexus-scholar/core-csharp/actions/runs/28402404840
```

Latest known local post-merge smoke for `main`:

```text
dotnet build NexusScholar.Core.slnx -c Release
dotnet test NexusScholar.Core.slnx -c Release --no-build
dotnet run --project samples/NexusScholar.Avalonia.Blocks.SampleHost
```

Result:

- build passed;
- tests passed;
- sample host launch smoke passed and closed cleanly.

## Current Public Position

Nexus Scholar Core is ready for:

- architecture feedback;
- developer critique;
- methodology critique;
- first-tester onboarding feedback;
- sample-host visual inspection feedback.

It is not ready for:

- real systematic-review execution by non-developers;
- live scholarly provider use;
- PDF extraction or OCR;
- persistence/API/cloud behavior;
- product desktop-shell evaluation;
- PHP compatibility claims.

## Current Gaps

High priority:

1. Add a fresh sample-host screenshot or GIF to the `gh-pages` getting-started tutorial.
2. Add root `LICENSE`, `CONTRIBUTING.md`, and `SECURITY.md`.
3. Add README links to the public tutorial and issue templates.
4. Add CI or script smoke for `doctor`, `sample`, and `demo`.

Next architecture planning:

1. Draft APP-01 as ADR 0015 or equivalent planning doc.
2. Scope AppServices as read-only composition from Search Import + Deduplication into `WorkspacePlan`.
3. Keep Core UI-free and keep Avalonia renderer Core-free.

Planning only:

1. Provider/network/legal questions.
2. Credential/rate-limit/reproducibility rules.
3. No provider implementation until a later accepted ADR.

## Next Recommended Branches

Suggested order:

1. `cdx/public-tester-polish`
2. `cdx/cli-public-path-smoke`
3. `cdx/appservices-readonly-composition-adr`

Keep each branch narrow. Do not combine provider implementation, persistence, API/cloud, PDF/OCR, or product-shell behavior into these packets.

## Non-Claims To Preserve

- no live provider behavior;
- no provider SDKs or credentials;
- no HTTP download behavior;
- no Google Scholar scraping;
- no paywall bypass or shadow-library source;
- no persistence/API/cloud;
- no PDF extraction;
- no OCR;
- no PHP compatibility claim;
- no Core dependency on UI frameworks;
- no sample host as product shell.
