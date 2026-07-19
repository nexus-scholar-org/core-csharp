# Release Readiness Alpha 2 Completion Evidence

Status: complete, merged, published, and independently verified.

Date: 2026-07-19

Authority:

- ADR 0046;
- accepted Release Readiness Alpha 2 gate;
- the protected-main commit resolved by `v0.1.0-alpha.2`;
- `desktop-distribution-manifest.json` for exact source and artifact identity.

## Protected-Main Delivery

- Implementation: [PR #72](https://github.com/nexus-scholar-org/core-csharp/pull/72).
- Linux release-evidence repair:
  [PR #73](https://github.com/nexus-scholar-org/core-csharp/pull/73).
- Release commit: `96586395865ae5e4e976c15d5871a12be5578962`.
- Protected-main [Ubuntu and Windows gate](https://github.com/nexus-scholar-org/core-csharp/actions/runs/29677499983):
  passed.
- Protected-main [CodeQL](https://github.com/nexus-scholar-org/core-csharp/actions/runs/29677499974):
  passed.
- Non-publishing [release rehearsal](https://github.com/nexus-scholar-org/core-csharp/actions/runs/29677503733):
  passed on the release commit.
- Tag-bound [release workflow](https://github.com/nexus-scholar-org/core-csharp/actions/runs/29677685046):
  passed Core validation, Windows distribution validation, attestation,
  publication, and downloaded-byte verification.
- Prerelease:
  [Nexus Scholar Desktop 0.1.0-alpha.2 Technical Preview](https://github.com/nexus-scholar-org/core-csharp/releases/tag/v0.1.0-alpha.2).

## Delivered Scope

- RR-01: accepted Windows x64 technical-preview distribution, recovery,
  publication, and nonclaim contract.
- RR-02: current repository, product, security, UI, site, changelog, and
  version-specific release documentation.
- RR-03: self-contained portable desktop ZIP, dedicated locked runtime graph,
  repeated-publish inventory comparison, exact distribution manifest,
  checksums, SPDX SBOM, extracted-host smoke, and tag-only publication policy.
- RR-04: lock-aware manifest backup, verified byte-exact new-directory restore,
  failure cleanup, sanitized bounded local crash diagnostics, and next-launch
  recovery notice.
- RR-05: rendered Avalonia headless acceptance through initialize, import,
  analyze, verify, backup, restore, reopen, failure recovery, keyboard focus,
  pointer input, automation names, and 100%, 125%, and 150% scaling.
- RR-06: split Ubuntu and Windows release validation, artifact attestation,
  immutable-release enforcement, downloaded-asset byte comparison, and
  exact-tag prerelease publication.

## Local Verification

The complete pre-commit gate passed on Windows under pinned SDK `10.0.301`:

- 60-project Release build: zero warnings and zero errors;
- full solution: 1,111 passed, zero failed, four expected skips;
- Architecture: 45 passed;
- Conformance: 142 passed;
- desktop acceptance: 2 passed, including rendered scaling and input;
- scientific-invariant manifest: 150 exact cases across nine projects;
- reproducible package validation: 24 packages with clean local-source smoke;
- release evidence: 28 artifacts and 60 lock files;
- portable desktop: 268 files, two-publish inventory match, extracted clean
  smoke, and exact five-file release root;
- release-policy regressions: tag reuse, wrong RID, branch publication,
  existing-release mutation, dirty-manifest publication, and URI-based
  filesystem paths all rejected;
- CLI doctor, sample, and deterministic local demo: passed;
- pinned-SDK format verification: passed;
- public Astro site verification: 49 source files, 45 generated pages, and zero
  distribution issues.

The four skips are two live-provider probes that require explicit opt-in and
two Linux-only path assertions on the Windows host. Default CI remains free of
live scholarly-provider calls. Dirty working-tree evidence remains
validation-only. The tag workflow regenerated the release artifact from the
clean release commit with `sourceTreeDirty=false`.

## Independent Review

Independent scientific-invariant, architecture, conformance, and test-gap
reviews reproduced and closed findings in:

- release identity and clean-source enforcement;
- downloaded-asset and immutable-release verification;
- truthful measured desktop smoke output;
- backup/restore failure injection and promotion collision behavior;
- diagnostic redaction, test-only boundaries, and timestamp uniqueness;
- exact release-root inventory and publication ordering;
- rendered input, hit testing, focus, and scaling acceptance.

No local code or test finding remains open.

## Release Closure

All closure conditions hold for the exact release commit:

1. protected-main `analyze`, `review`, Ubuntu, and Windows checks are green;
2. `v0.1.0-alpha.2` resolves to that protected-main commit;
3. the distribution manifest records that commit and a clean source tree;
4. the GitHub prerelease is neither draft nor mutable through this workflow;
5. the release exposes exactly the ZIP, manifest, checksums, SPDX SBOM, and
   SBOM-validation assets;
6. downloaded bytes match local publication bytes and `SHA256SUMS.txt`; and
7. GitHub artifact attestation verifies for all five released assets.

Independent post-publication verification downloaded exactly five assets,
matched every GitHub asset digest, verified all four non-circular checksum
entries, confirmed a 268-file clean-source manifest, and verified GitHub
attestation for the ZIP, manifest, checksums, SPDX SBOM, and SBOM-validation
record. The ZIP digest is
`sha256:2f182514d62f09f8602aaa4e53702ebc739f2515bd893f9aa8fec9be4dbc77b3`.

The authoritative release endpoint is:

`https://github.com/nexus-scholar-org/core-csharp/releases/tag/v0.1.0-alpha.2`

## Invariants And Compatibility

- Scientific authority remains in immutable, digest-bound Core records.
- Backup, restore, diagnostics, manifests, UI values, and release metadata do
  not become scientific authority.
- Human preview and confirmation remain required for admitted desktop
  mutations.
- No golden fixture or `specs/SOURCE.lock.json` entry changed.
- No PHP compatibility claim is added.

## Nonclaims

This release does not authorize production, compliance, accessibility
certification, authenticated-user, multi-user, installer/update,
signed-publisher, NuGet publication, provider-completeness, PDF/OCR,
plugin-runtime, AI-runtime, database, API, cloud, or support-SLA claims.
