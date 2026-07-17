# ADR 0039: Live Search Provider Acquisition Contract

Status: Accepted

Date: 2026-07-17

## Context

- Sources applied in repository authority order:
  - `docs/adr/0010-search-trace-and-plan-contract.md`
  - `docs/adr/0011-search-import-source-contract.md`
  - `docs/adr/0014-fulltext-acquisition-artifact-and-extraction-contract.md`
  - `docs/adr/0027-phase-7-citation-network-dissemination-evidence-boundary.md`
  - existing fixtures under `fixtures/conformance/search/`
  - pinned PHP Search behavior and cassette evidence
  - current `NexusScholar.Search` implementation
  - `docs/plans/2026-07-14-feature-expansion-priority.md` FE-09
  - `docs/port/php-search-behavior.md`
  - `docs/port/php-search-fixture-plan.md`

Crossref's current official documentation identifies `https://api.crossref.org`
as the REST base, exposes `/works` as the searchable work endpoint, permits
public access, recommends identified polite access, publishes response rate-limit
headers, and requires callers to back off after `429`. Those operational rules
can change independently of Core and therefore belong to an outward host:

- <https://www.crossref.org/documentation/retrieve-metadata/rest-api/>
- <https://www.crossref.org/documentation/retrieve-metadata/rest-api/access-and-authentication/>

## Decision

### 1. Scope and sequence

FE-09A scopes the acquisition contract boundary for live Search sources and the
first provider-specific outward normalization adapter.

The first implementation slice is:

- provider-neutral request, page, response-evidence, and attempt contracts in
  `NexusScholar.Search`;
- a non-packable `NexusScholar.Search.Providers.Crossref` outward project that
  constructs sanitized Crossref request descriptors and parses exact recorded
  response bytes;
- no HTTP transport, runtime network call, retry scheduler, credential resolver,
  cache, or production host registration in this slice.

FE-09A accepts only exact response bytes retained in local repository fixtures.
No runtime response, provider-terms retention choice, or live acquisition record
can be admitted until legal-access, data-retention, network, credential, and host
policies are accepted under FE-09D and the relevant provider successor gate.

### 2. Provider-neutral acquisition contracts

All live-provider interaction is normalized behind one stable provider-neutral contract so no provider SDK or API shape can become Core authority.

Contract schemas for FE-09A are:

- `nexus.search.provider-acquisition-request / 1.0.0`
- `nexus.search.provider-page-request / 1.0.0`
- `nexus.search.provider-page-result / 1.0.0`
- `nexus.search.provider-attempt-evidence / 1.0.0`
- `nexus.search.provider-raw-response / 1.0.0`

Core records never contain credentials, contact addresses, authorization headers,
raw response URLs, or authentication tokens. A sanitized request descriptor may
retain the provider alias, endpoint path, non-secret query parameters, and their
canonical digest. A host must reject or redact secret-bearing material before it
can enter these contracts.

Recorded fixture acceptance identifies the local human or fixture-generation
agent that accepted the exact bytes, the acceptance timestamp, source note, and
raw-byte digest. This is fixture provenance, not a claim that the accepting actor
authored or verified the provider metadata.

### 3. Recorded-fixture-backed outward adapter (single first adapter)

FE-09A admits exactly one provider-specific outward adapter project in the first
slice: `NexusScholar.Search.Providers.Crossref`.

This adapter:

- builds a sanitized `/works` request descriptor from a validated provider page
  request;
- accepts exact response bytes supplied by its caller;
- deterministically normalizes Crossref `message.items` into raw Search sightings;
- emits response digest, parser identity, pagination, warning, and completeness
  evidence;
- never opens files, resolves credentials, sends requests, sleeps, or reads the
  wall clock.

Recorded fixture tests supply the exact bytes. The same parser may later receive
bytes from an admitted host transport without changing Core authority. Any
adapter that can issue network calls is out of scope for this slice.

### 4. Pagination policy in scope

Provider contract must carry:

- `page_size`
- `page_token_or_offset`
- `page_index`
- `max_results`
- `next_page_token_or_offset`
- `is_last_page`

Page and attempt evidence must include:

- selected query, year range, language, max results, offset, and include-raw flag;
- provider alias used;
- normalized provider aliases and execution policy version used;
- attempt order, injected request/response timestamps, partial-page shape, and
  completion state.

### 5. Retries and rate limits in scope

Attempt evidence may include only observed facts supplied by a retained fixture:

- attempt ordinal and outcome category;
- observed HTTP status when supplied;
- response category;
- parsed `retry_after` when supplied;
- observed rate-limit header values when supplied;
- partial-page and stop reason;
- exact request and raw-response evidence digests.

FE-09A does not classify retry eligibility and does not decide retry count,
backoff, jitter, concurrency, credential use, throttling, caching, or freshness.
Those are FE-09D/FE-09E decisions. Tests do not simulate sleeping.

### 6. Raw response evidence and completeness

Every successful parse and every response-bearing failure includes raw response
evidence with:

- provider alias
- fixture/source id
- SHA-256 digest over exact bytes using `raw-artifact-bytes`;
- exact byte length, media type, injected receipt timestamp, and observed status;
- retention disposition fixed to `retained-local-fixture`;
- parser failure or partial reason where applicable.

Fixture bytes are retained. Runtime retention is not decided by this ADR. Raw
responses are evidence only and never prove scientific correctness.

### 7. Credential and secret handling

Core records must never store provider credentials or API keys.

Any contact or secret-bearing context required by a provider remains outside
Core and is owned by the future FE-09D host policy. FE-09A does not define a
credential-reference schema.

### 8. Compatibility and non-claim boundary

FE-09A makes no PHP or Crossref parity claim. Its fixtures prove only the local
request descriptor and parser behavior against their exact recorded bytes.

PHP-compatible behavior is represented only as evidence, not as authoritative contract parity. No provider-specific normalization, no live scraper parity, and no imported parser/API parity claims are admitted in FE-09A.

### 9. Fixture-first implementation boundary

FE-09A requires fixture-backed evidence for:

- pagination request/response progression
- observed rate-limit and retry-after evidence
- partial page and truncated fixture handling
- response-status failure and partial-page evidence
- unknown alias pre-validation evidence
- secret-bearing query, header, contact, and raw-URL rejection
- exact fixture-byte mutation and declared-digest mismatch
- parser-version mismatch
- provider result drift or pagination-chain mismatch between retained pages

## Alternatives considered

- Live adapter first with direct HTTP calls.
  - Rejected because it would introduce secrets, network nondeterminism, and replay gaps before contract stabilization.
- Provider-specific contracts per adapter in Core.
  - Rejected because it leaks provider shape into Core and blocks dependency direction.
- Generic fixture-player adapter.
  - Rejected because it would test replay machinery without proving a real
    provider request/normalization boundary.
- Require full raw-response retention for every provider.
  - Rejected because provider terms may restrict retention. Exact-byte retention
    is required for repository fixtures; runtime evidence must disclose whether
    bytes or only a digest were retained.

## Consequences

Positive:

- FE-09A remains deterministic and fixture-replayable.
- Downstream Search trace and plan contracts keep their raw evidence model without hard-coding provider APIs.
- Pagination/retry/rate/partial-completeness evidence becomes explicit and auditable.

Negative:

- No live source retrieval in first slice.
- No transport or host registration exists yet; production provider onboarding
  requires accepted legal/data-retention policy, FE-09D, and a successor FE-09A
  transport gate.
- Search parity remains limited to fixture-bounded provider behavior.

## Dependency order

1. FE-09A-1: accept provider-neutral acquisition request/execution contracts.
2. FE-09A-2: implement provider request/page/attempt/raw-response contracts.
3. FE-09A-3: implement deterministic Crossref request description and recorded-byte parsing.
4. FE-09A-4: add conformance fixtures and adapter-level negative cases.
5. FE-09A-5: gate closure and evidence bundle with explicit non-claims.

## Migration effect

No existing persisted C# Search records are migrated by ADR 0039.

Existing search traces and import traces remain authoritative only by their current contracts and are not reinterpreted as live-provider traces.

## Fixture effect

FE-09A fixture families must include:

- `search-crossref-recorded-page.response.json`
- `search-crossref-partial-page.response.json`
- `search-crossref-rate-limit-response.response.json`
- `search-crossref-schema-drift.response.json`
- retained-byte mutation against a declared fixture digest

These are local hand-authored conformance fixtures with source notes, exact raw
response digest, and local comparator rules. They are not PHP-generated goldens
and do not claim that a current live response will be identical.

## Reversal conditions

Revise this ADR only if:

1. a later FE-09D gate admits a concrete transport, credential-reference, and
   host-policy implementation;
2. a later ADR changes the provider evidence schema IDs or scope;
3. accepted provider terms authorize and require a runtime raw-response retention
   model;
4. hard legal or policy constraints require provider-specific runtime behavior to be represented in Core contracts.

## Explicit non-claims

- no live HTTP calls in CI
- no credentials or API keys in Core records or logs
- no Crossref compatibility claim beyond local fixture-backed parsing
- no scraping (including Google Scholar scraping)
- no paywall bypass, shadow-library, or unauthorized source-hijack behavior
- no PHP-compatibility claim beyond fixture-backed evidence
- no Search-time Deduplication changes
- no persistence/API/UI/cloud changes
- no AI governance or search-provider recommendation claims
