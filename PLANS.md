# Plans

This file keeps the historical gate map and points to the current operating plan.

## Current Operating Plan

The first public-feedback runway is now mostly in place:

- public-feedback issue templates and PR template are merged;
- the deterministic local CLI `demo` is merged and documented;
- the public getting-started walkthrough is merged on `gh-pages`;
- the Avalonia sample host presentation and scrolling polish is merged.

Current recommended work:

1. Refresh stale ops/review docs so they match `main` at `ac0307c` and `gh-pages` at `32475f4`.
2. Finish public tester polish: sample-host screenshot/GIF, root contributor/license/security docs, README links to public tutorial and feedback templates.
3. Add CI or script smoke for the public CLI path: `doctor`, `sample`, and `demo`.
4. Plan APP-01 as a narrow read-only AppServices composition ADR before implementation.
5. Keep provider work planning-only until a provider/network/legal ADR exists.

Do not implement providers, persistence, API/cloud behavior, PDF/OCR, live HTTP, or a UI product shell under the current plan.

## Current Detailed References

- `docs/ops/BRANCH-BOARD.md`
- `docs/ops/MERGE-QUEUE.md`
- `docs/ops/FIRST-PUBLIC-FEEDBACK-PLAN-2026-06-29.md`
- `docs/reviews/2026-06-29-main-public-readiness/README.md`

## Historical Implementation Gates

The gates below remain useful historical structure and source-routing context. They are not a command to restart Gate 0.

### Gate 0: evidence freeze

Map the blueprint and PHP reference, capture the PHP commit, define product laws, list open conflicts, and plan golden fixtures.

### Gate 1: repository quality

Keep restore, release build, tests, formatting, and architecture checks green on Windows and Linux.

### Gate 2: deterministic kernel

Implement typed identifiers, clocks, ID generation, canonical serialization, digests, errors, and actor identity.

### Gate 3: protocol lifecycle

Implement drafts, structured decisions, approval, immutable versions, amendments, waivers, and deviations.

### Gate 4: workflow compiler

Implement templates, parameters, nodes, edges, gates, validation, capability requirements, and invalidation planning.

### Gate 5: artifact and provenance ledger

Implement immutable artifacts, append-only events, agents, activities, inputs, outputs, and decision lineage.

### Gate 6: portable bundle

Export, verify, import, tamper-check, and round-trip the canonical review bundle.

### Gate 7: local application

Future gate. Add local application behavior only after application-service boundaries are explicit. Do not jump directly to persistence from the current public-feedback lane.

### Gate 8: first method pack

Future gate. Implement a Rapid Review pack with explicit shortcuts, consequences, mitigations, approvals, and reporting evidence.

### Gate 9: PHP behavior port

Port scholarly identity, normalization, deduplication, snapshots, screening, search, retrieval, graphs, and exports through differential fixtures.

Current Gate 9 local state includes Search, Search Import, Deduplication, Screening, and local no-network Full Text. PHP compatibility remains unclaimed without generated fixtures and comparators.

### Gate 10: plugins

Add capability-scoped official plugins, then an out-of-process host for third-party extensions.

### Gate 11: governed AI

Start with protocol clarification proposals. Add later AI tasks only after context, evidence, authority, validation, retention, and human-action policies are explicit.
