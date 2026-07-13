# ADR 0027: Phase 7 Citation Network And Dissemination Evidence Boundary

## Status

Accepted

## Date

2026-07-13

## Context

Hardening 29 requires generated evidence for Citation Network and dissemination-export behavior at the PHP commit pinned by `specs/SOURCE.lock.json`. The pinned package contains citation graph construction, graph analysis, bibliography and graph serializers, export handlers, persistence adapters, and export history records.

The C# repository has no `NexusScholar.Network` or `NexusScholar.Reporting` project. `docs/port/PORT-MATRIX.csv` marks both surfaces as planned and not implemented. ADR 0009 defines only the local portable review-bundle contract; ADR 0011 governs imported Search evidence; ADR 0014 governs local Full Text evidence. None authorizes Citation Network or dissemination-export production behavior.

Phase 7 is a compatibility-evidence phase under a feature freeze. Adding graph or reporting behavior before an accepted domain contract would silently promote PHP application behavior into C# authority. Omitting the observations would leave a material compatibility surface undocumented.

## Decision

For Hardening 29, deterministic PHP Citation Network and dissemination-export observations are retained as generated evidence and classified as `intentional_change` under `intentional-non-adoption-no-csharp-replay`.

This means:

- the exporter may invoke deterministic PHP domain builders, graph records, format vocabularies, and local serializers without network, Composer, Laravel, persistence, or external graph packages;
- C# has no semantic replay target and makes no Citation Network or dissemination-export compatibility claim;
- comparators validate provenance, digests, inventory, classifications, source references, observed PHP output, and the continued absence of `NexusScholar.Network` and `NexusScholar.Reporting`;
- Search imports, review bundles, Research Workspace reports, and Full Text artifacts are not substitutes for PHP dissemination exports;
- PHP graph ids, runtime object hashes, timestamps, persistence rows, storage paths, and external graph-library output are not adopted as C# scientific identity or authority;
- future implementation requires accepted Citation Network and Reporting contracts before production code or equivalence claims.

This closes the Phase 7 evidence boundary. It does not define graph identity, snapshot equality, metric reproducibility, bibliography canonicalization, export-history authority, or reporting persistence.

## Alternatives Considered

### Implement Network And Reporting During H29

Rejected. This would expand product behavior without accepted contracts for graph identity, metrics, snapshots, export authority, or persistence.

### Treat Existing Bundle Or Workspace Exports As Equivalent

Rejected. Review bundles, imported Search files, and workspace reports have different authority, identity, and provenance contracts.

### Leave The Cases Unresolved

Rejected for Phase 7 classification. The deliberate decision is to preserve PHP observations without adopting them. Future product semantics remain open work, not a conflict blocking evidence closeout.

### Omit The Evidence

Rejected. H29 explicitly requires graph/export observations.

## Consequences

Positive:

- Phase 7 records the remaining PHP graph/export surface without overstating compatibility.
- Feature expansion remains frozen until contracts are accepted.
- Future design starts from reproducible evidence.

Negative:

- C# still provides no Citation Network or dissemination-export implementation.
- All H29 cases demonstrate intentional non-adoption rather than parity.
- PHP metrics, persistence, and serializer behavior outside the generated cases remain unclaimed.

## Migration Effect

No production data, schema, package, API, or runtime migration is introduced. Future implementations must not reinterpret Search imports, workspace reports, bundle artifacts, or PHP persistence rows as canonical graph/export records.

## Fixture Effect

The H29 fixture set may include graph/export vocabularies, namespace-qualified nodes, edge validation, direct/co-citation/coupling observations, BibTeX serialization, local GraphML serialization, and filename-extension validation. Every case must cite this ADR, use `intentional_change`, state that no C# replay target exists, and avoid nondeterministic ids, timestamps, runtime object hashes, external graph packages, persistence, and network behavior.

## Reversal Conditions

This boundary may be replaced only after accepted Citation Network and Reporting ADRs define stable identities, authority, provenance, snapshot/equality rules, deterministic metric requirements, serialization contracts, and persistence-independent Core records. H29 classifications must then be reviewed before any compatibility claim is widened.
