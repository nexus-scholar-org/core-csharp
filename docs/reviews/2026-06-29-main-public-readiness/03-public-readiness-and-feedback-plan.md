# Public Readiness And Feedback Plan

Status: current after public-feedback onboarding, deterministic CLI `demo`, `gh-pages` first-tester walkthrough, and Avalonia sample-host presentation polish.

## Honest Public Position

Use this public framing:

> Nexus Scholar Core is an audit-grade, local-first C# research workflow kernel. It currently proves strict records, authority boundaries, conformance fixtures, local no-network Full Text evidence, deterministic CLI demo output, structured feedback routing, and a sample block-rendering path. It is not yet a finished researcher app.

Do not pitch it as:

- a production systematic-review app;
- an AI paper summarizer;
- a full desktop product;
- a live scholarly-provider tool;
- a PDF extraction/OCR tool;
- a cloud collaboration platform;
- PHP-compatible behavior.

## What Is Better Now

- `main` contains Full Text implementation, UI contracts, Avalonia renderer, sample host, public feedback templates, and CLI `doctor`, `sample`, and `demo`.
- `gh-pages` contains a real first-tester getting-started tutorial instead of the old placeholder.
- Remote branch state is clean: only `main` and `gh-pages`.
- README describes the current surface, demo output, sample host, authority rules, and non-claims.
- Hosted CI is green on Ubuntu and Windows for `main` at `ac0307c`.

This makes the project safe to show for architecture/developer/methodology feedback. It does not make it ready for general researcher use.

## Who To Ask For Feedback First

The best first testers are not general researchers yet. The right first circle is:

1. Developers who care about scientific/research tooling.
2. Systematic-review methodologists who can critique authority and audit boundaries.
3. PhD students or researchers who have experienced messy search/dedup/screening workflows.
4. Open-source maintainers who can review contributor onboarding.
5. One or two UI-minded people who can run the sample host and critique block clarity.

## What To Ask Them To Do

Do not ask: "Can you use Nexus for your review?"

Ask concrete tasks:

1. Read the README and tell me what you think the project does.
2. Run the verification commands and report whether setup succeeds.
3. Run CLI `doctor`, `sample`, and `demo`; explain what is unclear.
4. Open the public getting-started tutorial and say where it loses you.
5. Run the Avalonia sample host and inspect the sample workspaces.
6. Open the Deduplication, Screening, or Full Text module docs and identify one unclear boundary.
7. File one structured feedback issue using the GitHub templates.

## Current First Feedback Loop

Already present:

- public getting-started tutorial;
- issue templates;
- PR template;
- CLI `doctor`, `sample`, and deterministic local `demo`;
- README expected-output summary for the demo;
- sample-only Avalonia host.

Still needed:

- sample-host screenshot or GIF in the public tutorial;
- root `LICENSE`, `CONTRIBUTING.md`, and `SECURITY.md`;
- README links to the public tutorial and issue templates;
- CI or script smoke for the public CLI path;
- a pinned or clearly linked first-feedback issue after the repo is ready for external traffic.

## Public Site Review

What is already good:

- static site exists on `gh-pages`;
- homepage has clear positioning;
- blog posts explain motivation and market distinction;
- architecture page is strong;
- module pages exist for current modules;
- first-tester getting-started tutorial exists;
- internal links passed a local static link check in the external review;
- public docs are mostly honest about non-claims.

What still blocks a more polished first-tester invitation:

- no sample-host screenshot/GIF in the walkthrough;
- no root `LICENSE`, `CONTRIBUTING.md`, or `SECURITY.md`;
- no CI smoke for the exact public CLI commands;
- no public roadmap page tied to current `origin/main`;
- no current visual asset showing the fixed scrollable sample host.

## Repo Landing Page Review

The repo README is now acceptable as a developer-facing entry point. It should link directly to:

- the public getting-started tutorial;
- the first-tester issue template;
- the architecture-boundary issue template;
- the documentation-confusion issue template;
- the sample-host screenshot/GIF after it exists.

The README still should not be the only public onboarding path. The public site should remain the guided walkthrough.

## Feedback Channels Present

Current `.github/ISSUE_TEMPLATE/` files:

- `first-tester-run.yml`
- `architecture-boundary-review.yml`
- `research-workflow-use-case.yml`
- `documentation-confusion.yml`
- `bug-report.yml`

Current PR template:

- `.github/PULL_REQUEST_TEMPLATE.md`

Follow-up:

- verify labels exist in GitHub, or document that labels are optional.
- add a pinned first-feedback issue after root contributor docs are present.

## What To Show First

Best first public artifact:

> A short public walkthrough: "Run Nexus Scholar Core, inspect a deterministic local import/dedup demo, and see why AI suggestions are not decisions."

It should show:

- hosted `gate-01` green;
- local build/test/format path;
- `dotnet run --project src/NexusScholar.Cli -- doctor`;
- `dotnet run --project src/NexusScholar.Cli -- sample`;
- `dotnet run --project src/NexusScholar.Cli -- demo`;
- the demo's stable local/no-network import and dedup summary;
- sample host screenshot/GIF;
- `samples/block-plans/dedup-review.sample.json`;
- one module page link;
- one issue-template link.
