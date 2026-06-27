# PHP Deduplication Behavior Map

Status: reconnaissance and planning only. No C# Deduplication behavior is implemented by this document.

Pinned PHP source:

- Repository: `../core`
- Commit: `b24d0d71ec7b64003465182477e7edb7f49994f4`
- Source lock: `specs/SOURCE.lock.json`

Local note: the pinned PHP checkout was inspected at the locked commit. The checkout contains unrelated local `composer.json` and `composer.lock` modifications; those files were not edited for this reconnaissance.

## Sources Read

- `../core/docs/v1.0/modules/04-core-deduplication-and-corpus-lock.md`
- `../core/docs/v1.0/modules/02-core-shared-kernel.md`
- `../core/docs/v1.0/modules/03-core-search-and-providers.md`
- `../core/src/Deduplication/**`
- `../core/src/Shared/Domain/CorpusSlice.php`
- `../core/src/Shared/Domain/ScholarlyWork.php`
- `../core/src/Shared/ValueObject/WorkId*.php`
- `../core/src/Laravel/NexusServiceProvider.php`
- `../core/tests/Unit/Deduplication/**`
- `../core/tests/Feature/Persistence/*Dedup*`
- `../core/tests/Feature/Persistence/*CorpusSnapshot*`
- `../core/tests/Feature/Laravel/DeduplicateCorpusJobTest.php`
- `../nexus-cli/app/Search/**`
- `../nexus-cli/tests/Feature/Commands/NexusSearchTest.php`
- `../nexus-web/docs/workflow-5-dedup-corpus-lock.md`
- `../nexus-web/app/Actions/Projects/RunProjectCorpusDeduplication.php`
- `../nexus-web/app/Actions/Projects/BuildProjectCorpusSlice.php`
- `../nexus-web/app/Actions/Projects/LockProjectCorpus.php`
- `../nexus-web/app/Actions/Projects/ProjectCorpusMembershipHasher.php`
- `../nexus-web/app/Queries/Projects/ProjectDeduplicationReadModel.php`
- `../nexus-web/tests/Feature/ProjectDeduplicationWorkflowTest.php`

## Behavior Summary

PHP Deduplication is not a single identity comparison. It is a pipeline:

1. receive a `CorpusSlice`
2. run ordered duplicate policies
3. union duplicate pairs into transitive clusters
4. preserve pair evidence
5. elect one representative per cluster
6. expose representative-only corpus output
7. optionally persist clusters and lock project corpus snapshots through Laravel ports or host-app code

The PHP implementation predates the C# Search trace boundary. PHP Search and CLI flows often return already-collapsed `CorpusSlice` values, while Web deliberately rebuilds a rawer draft corpus with `fromWorksUnsafe()` before calling the core deduplicator. That difference is the main reason C# Deduplication must be planned against Search traces and imported sightings, not by copying PHP `CorpusSlice` usage mechanically.

## Input Shape

The PHP application command is `DeduplicateCorpus`, with:

- `corpus`: `CorpusSlice`
- `projectId`: string, defaulting to `default-project`
- `policyAliases`: optional list of policy names; empty means all registered policies

`DeduplicateCorpusHandler` reads `corpus->all()` and initializes union-find over each work's `primaryId()->toString()`. If a work has no primary id, the handler falls back to `spl_object_hash($work)` as the internal union key.

This fallback is incompatible with C# scientific identity rules from `ADR 0007`: runtime object identity must not become scientific identity. C# may use transient local row ids only as non-scientific trace/member handles, and no-id records must remain unresolved candidates unless a later ADR defines a stricter rule.

## CorpusSlice Pre-Deduplication

PHP `CorpusSlice::fromWorks()` calls `addWork()`, and `addWork()` merges records when `ScholarlyWork::isSameWorkAs()` finds any WorkId overlap. Therefore normal `CorpusSlice` construction can hide exact identifier duplicates before `DeduplicateCorpusHandler` sees them.

PHP has `CorpusSlice::fromWorksUnsafe()` to bypass that merge. Tests use it for duplicate fixtures, and Web uses it because Deduplication must inspect every draft member.

C# must explicitly decide the raw Deduplication input shape before implementation. The expected direction is:

- Dedup consumes Search traces or imported sightings.
- Dedup preserves raw sightings before clustering.
- Dedup emits clusters, representatives, and evidence.
- Dedup does not consume live provider APIs.
- Dedup does not become Search or Screening.

## Duplicate Evidence Shape

PHP duplicate evidence is `Duplicate`:

- `primaryId`: `WorkId`
- `secondaryId`: `WorkId`
- `reason`: `DuplicateReason`
- `confidence`: float

Reasons are:

- `doi_match`
- `arxiv_match`
- `openalex_match`
- `s2_match`
- `pubmed_match`
- `title_fuzzy`
- `fingerprint`

`Duplicate::isHighConfidence()` treats confidence `>= 0.95` as high confidence. `toArray()` serializes primary/secondary IDs, reason, and confidence.

The core evidence object does not include raw provider sighting ids, source query ids, source file digests, app membership hashes, or provenance rows. Those are host/search/import concerns and require C# contract decisions before implementation.

## Exact Identifier Matching

PHP exact matching policies:

- `DoiMatchPolicy`
- `NamespaceMatchPolicy(ARXIV)`
- `NamespaceMatchPolicy(OPENALEX)`
- `NamespaceMatchPolicy(S2)`
- `NamespaceMatchPolicy(PUBMED)`

Each exact policy indexes by normalized namespace value and emits a duplicate from the first-seen work to later works sharing that value. Confidence is `1.0`.

DOI values are normalized by `WorkId` construction; `https://doi.org/` and `doi:` prefixes are stripped and values are lowercased. arXiv prefixes are stripped and lowercased. Other namespaces are lowercased.

The PHP core policy set does not cover Scopus EID, Web of Science UT/accession numbers, MAG, PMCID as a duplicate reason, or other source-specific identifiers. Under `ADR 0011`, source-specific import ids remain source evidence unless a later ADR extends `WorkIdNamespace`.

## Title Fuzzy Matching

`TitleFuzzyPolicy`:

- normalizes title through `TitleNormalizer`
- sorts by normalized title
- compares adjacent pairs only
- skips empty normalized titles
- skips pairs when both years exist and the absolute year gap is greater than `maxYearGap`
- computes a Levenshtein-derived ratio over normalized strings
- emits `title_fuzzy` when ratio is at least threshold

The implementation has an important conflict:

- `TitleFuzzyPolicy` constructor default threshold is `92`.
- The PHP Laravel service registration binds `TitleFuzzyPolicy(new TitleNormalizer, 95)`.
- PHP module docs say the default Laravel binding sets the title fuzzy threshold to `95`.
- Nexus Web persisted run metadata writes `title_fuzzy: 0.95`.
- Nexus Web demo seed data still contains `title_similarity: 0.92`.

This must be decided before C# Deduplication implementation. C# should not claim PHP parity while the threshold source is unresolved.

## Fingerprint Policy

`FingerprintPolicy` exists but is not registered in the default Laravel binding documented for Deduplication. It hashes:

- normalized title prefix
- normalized first author family name
- year

It emits `fingerprint` confidence `0.90`. Because it is not part of the default Laravel binding, C# should treat fingerprint matching as future or explicit-policy behavior unless a Dedup ADR admits it.

## Transitive Clustering

`DeduplicateCorpusHandler` uses union-find:

- make a set for every input work key
- run each policy once
- skip pair evidence already produced by a higher-priority policy
- union duplicate pairs
- build groups from connected components
- assemble one `DedupCluster` per group
- re-absorb members by traversing recorded evidence for the group
- throw if recorded evidence does not connect every member

Transitive behavior is deliberate: if A matches B and B matches C, all three become one cluster even if A and C do not directly match.

Important comparator consequence: cluster membership should be compared as sets, while pair evidence should preserve the exact edges and reasons that created the connected component.

## Representative Election

PHP representative election uses `CompletenessElectionPolicy`:

- score = `ScholarlyWork::completenessScore()` + provider priority
- provider priority defaults:
  - `openalex`: 5
  - `crossref`: 4
  - `semantic_scholar`: 3
  - `arxiv`: 2
  - `pubmed`: 2
  - `ieee`: 1
  - `doaj`: 1
- tie-break 1: prefer DOI presence
- tie-break 2: earlier `retrievedAt`

`ScholarlyWork::completenessScore()` gives weight to DOI, abstract, venue, authors, year, cited-by count, non-retraction, and ORCID presence.

The second tie-break depends on runtime retrieval time unless fixtures inject stable times. C# fixtures should not anchor on unpinned wall-clock values.

## Merge Behavior

`DedupClusterCollection::toCorpusSlice()` emits one representative per cluster. Before adding representatives to the returned `CorpusSlice`, it merges non-representative member fields into the representative through `ScholarlyWork::mergeWith()`.

`mergeWith()` keeps fields from the representative and uses the other work only to fill missing fields:

- identifiers are unioned
- authors are filled only when representative authors are empty
- year, venue, and abstract fill null fields only
- cited-by count becomes the maximum non-null count

Existing representative fields are not overwritten.

## No-Id Candidate Behavior

PHP Deduplication can carry no-primary-id works internally by using runtime object hashes for handler keys and cluster absorption keys. PHP `CorpusSlice` also falls back to object identity when no primary id exists.

This is an intentional incompatibility for C# scientific identity. C# may keep no-id search/import records as unresolved candidates and may assign local trace member handles for processing, but it must not treat runtime object identity, local file paths, or title-only similarity as canonical scientific identity.

## Raw Duplicate Preservation

PHP has two contradictory patterns:

- PHP Search and CLI flows often collapse works into `CorpusSlice` before returning/caching.
- Web explicitly uses `fromWorksUnsafe()` so Deduplication can inspect every draft member and then persists raw query links, cluster members, evidence, representatives, and membership hashes.

C# Search already preserves raw traces. Deduplication should consume that raw trace evidence, preserve sightings, and only then emit clusters/representatives.

## Locked Corpus And Project Behavior

PHP core includes `CorpusLockPolicy` and lock handlers:

- `DeduplicateCorpusHandler` may call `lockPolicy->assertCorpusMutable(projectId, DEDUPLICATE)`.
- `LockCorpusHandler` locks the project and marks unlocked clusters locked inside a transaction.
- `UnlockCorpusHandler` unlocks project state through lifecycle ports.
- Persistence includes dedup clusters, cluster members, corpus snapshots, snapshot works, project lock lifecycle state, and locked flags.

Web strengthens the flow:

- dedup is blocked when project is locked
- lock requires fresh dedup evidence matching a membership hash
- stale membership blocks lock
- incomplete cluster/member evidence blocks lock
- lock creates a representative-aware snapshot
- screening requires the locked snapshot to be representative-aware

These are important app-alignment behaviors, but not all of them are core Deduplication behavior.

## PHP Persistence Behavior

PHP core has persistence ports and Laravel repositories for:

- clusters
- cluster members
- corpus snapshots
- snapshot works
- project lock state

Web persists additional run state:

- `project_corpus_dedup_runs`
- `membership_hash`
- input count
- representative count
- duplicate cluster count
- duplicate member count
- duplicates removed
- policy stats
- run metadata
- completion timestamp

C# Deduplication implementation should not add persistence unless a later gate explicitly admits persistence. Local C# Dedup should start with in-memory/domain behavior and conformance fixtures.

## CLI Behavior

`nexus-cli` Search writes a global deduplicated `all_*.json` master when running all queries. It reports:

- raw count
- unique count
- duplicate count
- kept percentage

The CLI implementation uses `CorpusSlice` merge behavior and strips raw data for global output. This is a CLI projection and not Core C# authority.

## Web Behavior

Nexus Web adds a project-facing Dedup workflow:

- run Deduplication from draft corpus
- persist duplicate clusters and members
- compute membership hash over query links, work fields, identifiers, and authors
- expose read model state: `not_run`, `clear`, `duplicates_found`, `stale`, `locked`
- require fresh dedup run before locking
- build representative-aware lock snapshot
- block Screening unless the locked snapshot is representative-aware

Web also extends core behavior:

- exact identifier grouping over persisted `work_external_ids`
- internal work id mapping
- representative fallback scoring if core representative is unavailable
- app-specific membership hash
- app-specific run records and audit rows

Those Web projections are not C# Core authority until an ADR or gate admits them.

## Behaviors To Port Locally

Recommended local C# Deduplication behavior, after a contract ADR:

- consume C# Search traces and imported sightings, not live providers
- preserve raw sightings before clustering
- exact identifier duplicate policies over ADR 0007 namespaces
- transitive cluster assembly
- stable duplicate evidence records
- representative election with deterministic tie-breakers
- representative merge/fill behavior without overwriting existing fields
- no-id unresolved candidate handling without runtime identity fallback
- locked-corpus rejection if a local lock/snapshot boundary is in scope
- deterministic fixture output independent of generated ids and wall-clock time

## Intentional Incompatibilities To Consider

Likely intentional C# differences:

- reject PHP runtime object identity fallback as scientific identity
- do not pre-deduplicate Search output before Deduplication
- do not treat CLI global `all_*.json` as Core authority
- do not treat Web membership hash as Core authority without an ADR
- do not implement persistence in the local domain gate
- do not implement manual merge/split/representative override in this gate
- do not claim PHP compatibility without generated PHP fixtures and comparators

## Required C# Decisions Before Implementation

Implementation readiness is **no** until these decisions are made:

- raw Deduplication input shape from Search traces and import records
- title fuzzy threshold and comparator policy
- title fuzzy algorithm and Unicode behavior
- no-id candidate processing rules
- representative election deterministic tie-breakers
- whether fingerprint policy is excluded, future, or explicit optional policy
- Core/App boundary for membership hashes, persisted runs, representative snapshots, and Web scoring
- fixture and comparator rules for generated ids, run durations, and timestamps

## Explicit Non-Claims

- no C# Deduplication implementation
- no generated PHP fixtures
- no PHP compatibility
- no Deduplication persistence schema
- no Search implementation change
- no Search import implementation change
- no Screening behavior
- no bundle behavior change
- no provider/network behavior
- no API/UI/cloud behavior
- no app behavior made authoritative
