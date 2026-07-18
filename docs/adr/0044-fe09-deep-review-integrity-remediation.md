# ADR 0044: FE-09 Deep Review Integrity Remediation

Status: Accepted

Date: 2026-07-17

## Context

The post-FE-09 deep review found integrity gaps that were not covered by the
PR #69 completion evidence:

- credential-shaped signed URL parameters could remain in recorded Full Text
  references;
- recorded retrieval evidence lacked strict canonical persistence and replay;
- retrieval failures and acquisition kinds could misstate observed facts;
- provider-cache index rebuild could race record promotion, and current policy
  resolution made historical entries unreadable after policy evolution;
- citation snapshots accepted caller-asserted corpus membership and silently
  omitted null collection entries;
- nested GitHub Pages fallback routes resolved relative links incorrectly.

The accepted FE-09 ADRs and gate records remain historical. This ADR owns the
successor remediation and its separate completion evidence.

## Decision

### Recorded Full Text retrieval

Recorded evidence uses
`nexus.fulltext.recorded-retrieval-evidence / 1.0.0`. Its canonical envelope
binds the canonical Full Text input digest and exact response bytes. Strict
rehydration requires the expected record digest, supplied input, exact bytes,
canonical JSON, exact schema fields, and raw-byte digest scope.

Source, rights, and redirect references reject credential-shaped query
parameter names, including common signed-URL forms. A terminal transport
failure remains the reported failure before rights admission can be evaluated.
Acquisition kind derives from the observed route and rights status; licensed or
unknown retrieval is not labelled open access.

### Provider evidence cache

The policy identity recorded on an entry remains historical evidence. A
successor policy does not make an older entry unreadable or unverifiable, but
only an entry matching the current allowed policy identity, retention mode, and
retention window can be served fresh.

Index rebuild and record promotion share a bounded exclusive lock. Lock
exhaustion produces the typed `provider-evidence-cache-store-busy` failure.

### Citation network

Snapshot construction requires a corpus-authority resolver. The resolved
authority must reproduce the requested corpus id and digest and the exact
ordered resolved-node identities. Null node or edge entries fail closed and are
never silently omitted.

### Public documentation

The static `404.html` uses root-relative local references, and distribution
validation rejects relative fallback references that would break on nested
routes.

## Consequences

Historical evidence remains reconstructable across cache-policy changes,
recorded Full Text evidence can be persisted and replayed strictly, citation
membership is authority-bound, and fallback documentation works from nested
routes.

This remediation introduces no live Full Text downloader, new provider,
scientific decision automation, cache authority, citation-impact claim, PHP
compatibility claim, or production-readiness claim.

## Verification

Completion evidence is recorded separately in
`docs/release/FE-09-DEEP-REVIEW-REMEDIATION.md`. The original FE-09 gate
evidence remains unchanged.
