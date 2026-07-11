# ADR 0019: Protocol supplemental authority records

Status: Accepted

Date: 2026-07-11

## Context

ADR 0003 defines Protocol waivers, amendments, and invalidation notices. ADR 0004 requires waiver and deviation approvals to follow the same actor, timestamp, digest-binding, and policy principles as Protocol-version approval, while using target-specific authority.

The current implementation stores approval ID strings on waivers and amendments without resolving approval records. Amendment construction does not prove complete previous/produced version lineage, and invalidation notices can be copied or replaced without an immutable membership proof. ADR 0018 therefore requires Workflow to reject these paths until Protocol exposes verified authority records.

There is no pinned PHP behavior or accepted persistence schema that supplies the missing proof. This ADR defines a local authority contract without making compatibility claims.

Decision owner: repository hardening lead acting under the user-authorized hardening plan.

## Decision

1. Waiver and amendment authority uses an immutable target-specific approval record carrying approval identity, target type, target ID, target content digest, policy identity and mode, decision, human actor, timestamp, optional role/rationale/supersession, and its own approval-record digest.
2. Supplemental approval records use `DigestScope.ApprovalRecord` with schema `nexus.protocol-supplemental-approval:1.0.0`.
3. Persisted approval claims remain unverified until the actor, policy, target identity, target digest, and approval-record digest are independently resolved and reproduced.
4. A verified waiver requires:
   - canonical waiver content and digest;
   - exact target type `protocol-waiver` and target ID equal to the waiver ID;
   - a resolved policy and exact satisfying approval set;
   - resolved human actors, required roles, and distinct-actor constraints.
5. A verified amendment requires:
   - canonical amendment content and digest;
   - exact target type `protocol-amendment` and target ID equal to the amendment ID;
   - a resolved policy and exact satisfying approval set;
   - an exact previous verified Protocol version and produced verified Protocol version;
   - matching protocol ID, previous digest, `amends_version_id`, `produces_version_id`, produced-version `amendment_id`, and supersession link;
   - unique, immutable invalidation notices whose `source_amendment_id` and affected requirements belong to the amendment.
6. Public waiver/amendment records are data claims, not authority. Only explicit verified wrappers may cross into Workflow authority.
7. Invalidation notices derive verified membership from the verified amendment; they are not independently caller-replaceable authority.

## Alternatives

### Continue counting approval IDs

Rejected. Identifier presence does not establish actor, target, policy, role, or digest authority.

### Reuse Protocol-version approvals

Rejected. ADR 0004 requires different target types. A Protocol-version approval cannot authorize a waiver or amendment.

### Define approval authority in Workflow

Rejected. Waiver and amendment authority belongs to Protocol; defining it outward would invert dependencies.

## Consequences

- Protocol gains reusable supplemental approval and rehydration proof types.
- Workflow can restore waiver/amendment compilation in a later gate by requiring verified wrappers.
- Existing waiver/amendment records remain usable as unverified DTO-like claims but cannot establish authority alone.
- No persistence, API, UI, provider, or generalized institutional policy engine is introduced.

## Migration Effect

- Callers persisting waivers or amendments must also persist target-specific approval records and their digests.
- Existing approval ID-only data cannot be upgraded silently; it must fail closed or be regenerated through an authorized human process.
- Existing historical fixtures remain unchanged. Hardening 05 adds separate replay recipes.

## Fixture Effect

Add deterministic cases for valid waiver and amendment authority, wrong target/digest/policy/actor/role, missing/extra/duplicate approvals, wrong previous or produced version, stale previous digest, replaced/duplicate/foreign invalidation notices, and mutable retained collections.

## Reversal Conditions

This contract may be replaced only by a later accepted ADR that preserves target-specific human authority, immutable history, digest reproducibility, and exact lineage.
