# FE-09 Deep Review Remediation Evidence

Status: complete on `cdx/fe-09-deep-review-remediation`; not yet merged.

Date: 2026-07-17

Authority:

- ADR 0044: FE-09 deep review integrity remediation;
- protected-main base `bdd0d828547773a622316988d8d3dc825c4e7812`;
- the repository commit containing this evidence record.

The original ADR 0041 through ADR 0043 and FE-09B, FE-09C, and FE-09E
completion records remain unchanged as historical PR #69 evidence.

## Repaired Findings

- Full Text recorded references reject common signed-URL secret parameters.
- Recorded retrieval evidence has a strict schema-versioned canonical codec
  that binds the admitted Full Text input and exact response bytes.
- Terminal transport failures retain their observed category, and licensed or
  unknown retrieval is not mislabelled open access.
- Provider-cache index rebuild cannot race record promotion and regress the
  latest pointer.
- Historical cache-policy entries remain readable and verifiable but cannot be
  served fresh under a successor policy.
- Cache lock exhaustion returns a typed bounded `StoreBusy` failure.
- Citation snapshots resolve exact corpus authority and reject null collection
  entries instead of silently dropping them.
- Nested GitHub Pages fallback references resolve from the site root and are
  enforced by distribution validation.
- Project state documents now identify protected main, PR #70, and the active
  remediation branch accurately.

## Verification

- `dotnet build NexusScholar.Core.slnx -c Release`: passed with zero warnings.
- `dotnet test NexusScholar.Core.slnx -c Release --no-build`: 1,024 passed,
  zero failed, two opt-in live smokes skipped.
- Full Text retrieval focused suite: 19 passed.
- Citation Network focused suite: 9 passed.
- Provider cache focused suite: 13 passed.
- `dotnet format NexusScholar.Core.slnx --verify-no-changes --no-restore`:
  passed.
- `scripts/verify-release-policy.ps1`: 24 approved validation-only packages
  passed.
- `npm run verify` under `site/`: 45 pages, 1,022 local references, zero
  validation issues.
- `npm audit --json`: zero known vulnerabilities.
- Opt-in live-provider smoke: OpenAlex passed; Semantic Scholar skipped because
  no S2 credential was configured.
- `git diff --check`: passed.

An independent scientific-invariant review found stale authority provenance and
two missing adversarial tests. ADR 0044 plus this record replaced mutation of
historical gate evidence; malformed canonical schema/scope and bounded cache
lock timeout tests were added and passed.

## Invariants

- historical accepted gate evidence is not rewritten to claim successor work;
- exact bytes and canonical source authority determine retrieval evidence;
- cache state and indexes remain operational evidence, not scientific truth;
- corpus membership comes from explicit authority resolution;
- failures remain evidence and do not become Screening decisions;
- CI remains network-free by default.

## Nonclaims

No live Full Text downloader, scraping, paywall bypass, PDF/OCR support,
Semantic Scholar body-retention right, citation completeness or impact metric,
provider parity, PHP compatibility expansion, package publication, deployment,
or production-readiness claim is introduced.
