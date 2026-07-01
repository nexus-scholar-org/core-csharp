# Chat Roster

Branch-derived Codex lane roster from current git state after public-feedback onboarding, `gh-pages` walkthrough, UI presentation merge, and branch cleanup.

## Active Lanes

- Lane `main`: current implementation baseline at `ac0307c`.
- Lane `gh-pages`: public documentation site at `32475f4`.
- Lane `cdx/state-refresh-public-comprehension`: docs-only state-refresh branch.

There are no active implementation `cdx/*` branches locally or remotely.

## Branch Containment Relationships

- `main` contains the implemented local review pipeline through Search, Import, Deduplication, Screening, and local no-network Full Text.
- `main` contains UI contracts, sample block plans, Avalonia renderer prototype, and the polished Avalonia sample host.
- `main` contains README, issue templates, PR template, local CLI `doctor`, `sample`, and deterministic `demo`.
- `gh-pages` contains the first-tester getting-started walkthrough.
- `gh-pages` remains separate public-site history.

## Status Notes

- Final hosted `main` CI for `ac0307c` is green on Ubuntu and Windows: https://github.com/nexus-scholar/core-csharp/actions/runs/28402404840
- Public feedback onboarding is merged on `main`.
- Public first-tester walkthrough is merged on `gh-pages`.
- The sample host is still a sample-only visual inspection harness, not a product shell.
- ADR 0014 defines the Full Text input boundary, acquisition records, source attempts, artifact evidence records, raw byte digest identity, extraction records, failure categories, legal/access boundary, app projection boundary, and Screening handoff.
- Local C# Full Text implementation is no-network only.
- Raw artifact identity is exact bytes plus `raw-artifact-bytes` digest.
- Derived extraction evidence must bind back to source artifact id and raw digest, and must not replace raw artifact evidence.
- PHP `pdf_fetches`, CLI manifests, Web batches/items, app audit rows, storage paths, and download routes are projections unless transformed into ADR 0014 records.
- Live providers, scraping, paywall bypass, shadow libraries, artifact storage, actual PDF parsing, OCR, and app behavior as Core authority remain unclaimed.

## Recommended Next Conversation

Focus next on first-tester polish and APP-01 planning:

1. capture and publish a sample-host screenshot/GIF on `gh-pages`;
2. add root `LICENSE`, `CONTRIBUTING.md`, and `SECURITY.md`;
3. add CLI public-path smoke to CI or `scripts/verify`;
4. draft ADR 0015 for read-only AppServices composition;
5. keep provider/network/legal work planning-only.

## Explicit Non-Claims For Next Lane

- no PHP compatibility
- no PHP-generated fixtures
- no persistence/API/cloud
- no live provider/network behavior
- no provider SDKs or credentials
- no paywall bypass
- no shadow-library source
- no Google Scholar scraping
- no actual PDF text extraction
- no OCR
- no artifact storage implementation
- no Screening behavior change
- no app behavior made authoritative
