# Continuation Roadmap

## Hard Recommendation

Do not jump straight into broad implementation.

The repository is now clean enough to show for architecture, developer, and methodology feedback. The next valuable work is to make the existing kernel understandable to first testers, then plan the first read-only application-service bridge. Otherwise, more correct internals will keep accumulating while public readers still cannot see how the parts connect.

## Completed Since Earlier Review

- `main` was consolidated and pushed.
- Full Text local no-network implementation was ported onto current `main`.
- Remote `cdx/*` branches were deleted.
- Local obsolete `cdx/*` branches and clean worktrees were removed.
- README and UI README were refreshed.
- Public feedback issue templates and PR template were added.
- CLI `demo` was specified, implemented, tested, and documented.
- `gh-pages` getting-started tutorial was replaced with a real first-tester walkthrough.
- Avalonia sample-host presentation and scrolling were polished.
- Hosted CI passed for `main` at `ac0307c`.

## Next Four Moves

### Move 1: Finish First-Tester Polish

Goal: let one external tester understand what they are seeing without reading the whole repo.

Tasks:

- add a sample-host screenshot/GIF to `gh-pages`;
- reference it from the getting-started tutorial;
- add root `LICENSE`, `CONTRIBUTING.md`, and `SECURITY.md`;
- add README links to the public tutorial and feedback templates;
- verify issue-template labels or document that labels are optional;
- add a pinned or clearly linked first-feedback issue.

Exit criteria:

- a tester can clone, verify, run CLI `doctor`/`sample`/`demo`, view the sample host, and file useful feedback.

### Move 2: Add CLI Public-Path Smoke

Goal: make the public commands part of the release gate.

Tasks:

- add CI or `scripts/verify` smoke for `doctor`, `sample`, and `demo`;
- keep the commands local-only and deterministic;
- make Bash and PowerShell verification paths as symmetric as practical.

Exit criteria:

- CI or the main verification script proves the README/public tutorial command path still runs.

### Move 3: Plan APP-01

Goal: bridge kernel truth to a tester-visible workflow without making Core depend on UI or persistence.

Best next slice:

> Read-only AppServices composition for Search Import + Deduplication into `WorkspacePlan`.

Why this slice:

- public testers can understand import warnings and duplicate review;
- it uses already-implemented Search Import and Deduplication;
- it turns Core evidence into `WorkspacePlan` without letting UI mutate Core;
- it prepares a real app without needing persistence/cloud.

Scope:

- ADR 0015 or equivalent planning doc first;
- read-only composition from existing Core records into `WorkspacePlan`;
- no persistence;
- no command execution;
- no real researcher project storage;
- no provider/network calls;
- tests asserting Core remains UI-free.

### Move 4: Implement The Smallest AppServices Slice

Goal: make the sample host display a real block plan generated from Core evidence through an application boundary.

Tasks after APP-01 is accepted:

- add `NexusScholar.AppServices` or the accepted equivalent;
- map Search Import + Deduplication results to `WorkspacePlan`;
- preserve evidence refs and validation refs;
- keep output deterministic;
- keep Avalonia renderer independent of Core.

Exit criteria:

- the renderer can consume generated `WorkspacePlan` output without making UI authoritative.

## What Not To Do Next

- Do not start cloud/persistence before app-service boundaries are explicit.
- Do not add live providers or HTTP clients.
- Do not turn the sample host into a real desktop app without an application-service command boundary.
- Do not pitch the project as ready for full systematic-review use.
- Do not treat Full Text as live retrieval, PDF extraction, OCR, or PHP-compatible behavior.
- Do not recreate branch sprawl without a live branch board.

## Brainstorm: Public Product Wedge

The strongest first wedge is not "AI summaries."

The wedge is:

> Evidence-preserving import and duplicate review for systematic review work, with audit-visible warnings, human gates, and verifiable exports.

Why:

- Search Import, Deduplication, Screening, Full Text, Bundles, and UiContracts support the concept.
- It is easier to demonstrate than full review execution.
- It exposes the project philosophy in a concrete way.
- It creates useful feedback from real researchers: "Does this match how messy search exports and duplicate candidates feel?"

First public demo path:

1. Import warning block.
2. Dedup candidate cluster block.
3. Human merge decision gate.
4. Bundle verification block.
5. Full Text evidence boundary note.
6. Non-authority labels and evidence refs.

The product story becomes:

> Nexus does not just summarize papers. It keeps the review workflow honest.
