# Nexus APP-01 generated local test bundles

Generated: 2026-07-01T00:00:00Z

This pack replaces unavailable manual exports from Scopus, Web of Science, and Google Scholar with **local source-like export files** that exercise the same parser/dedup/AppServices surfaces in Nexus Scholar Core.

Important non-claims:

- These are **not** subscription Scopus exports.
- These are **not** Web of Science exports.
- These are **not** scraped Google Scholar exports.
- The Google-Scholar-style `.bib` files are manually generated BibTeX-style fixtures.
- Some records are controlled synthetic mutations, clearly marked in `source_provenance.csv`.
- The pack is for local APP-01 testing only, not for scientific analysis or PHP compatibility claims.

## Repository validation note

`VALIDATION-DRY-RUN.json` is generator context, not an authoritative C# expectation file. The repository tests validate these bundles with the current `SearchImportService`, `DeduplicationService`, and `SearchDedupWorkspacePlanComposer` against `manifest.json`.

The current C# parser reports more detailed parser-warning counts than the dry-run file for some warning bundles. In particular, `FB04-warning-import` reports 7 parser warnings and `FB07-combined-app01-demo` reports 9 parser warnings because skipped-record and duplicate-source warnings are propagated in more than one parser surface. The manifest intentionally uses `parserWarningsAtLeast` or `parserWarningsExpected` for those bundles.

## Why this shape

ADR 0015 defines APP-01 as a read-only projection from `SearchImportTrace + DeduplicationResult -> WorkspacePlan`. These bundles are designed to drive:

1. import summary blocks;
2. import warning summary blocks;
3. exact duplicate cluster blocks;
4. review-required record-comparison blocks;
5. human merge-decision placeholder blocks.

## Supported import formats used

- `scopus-csv`
- `ris`
- `bibtex`

## Bundles

### FB01-clean-single-source — Clean single-source import

Audit-mode baseline: import summary with no parser warnings and no duplicate review candidates.

Files:
- `bundles/FB01-clean-single-source/scopus_like_clean.csv` — `scopus-csv`, source `scopus-like-public-metadata`, trace `trace-fb01-scopus-clean`

Expected coverage:

```json
{
  "importedRecords": 4,
  "sightings": 4,
  "parserWarnings": 0,
  "dedupExactClusters": 0,
  "dedupReviewCandidates": 0,
  "suggestedBlockMode": "Audit"
}
```

### FB02-cross-source-exact-duplicates — Cross-source exact duplicate cluster

Same DOI represented in CSV, RIS and BibTeX, to force an exact-identifier dedup cluster.

Files:
- `bundles/FB02-cross-source-exact-duplicates/scopus_like_rayyan.csv` — `scopus-csv`, source `scopus-like-public-metadata`, trace `trace-fb02-scopus-rayyan`
- `bundles/FB02-cross-source-exact-duplicates/wos_like_rayyan.ris` — `ris`, source `wos-like-public-metadata`, trace `trace-fb02-wos-rayyan`
- `bundles/FB02-cross-source-exact-duplicates/semantic_scholar_style_rayyan.bib` — `bibtex`, source `semantic-scholar-style-public-metadata`, trace `trace-fb02-s2-rayyan`

Expected coverage:

```json
{
  "importedRecords": 3,
  "sightings": 3,
  "parserWarnings": 0,
  "dedupExactClusters": 1,
  "dedupReviewCandidates": 0,
  "suggestedBlockMode": "Audit"
}
```

### FB03-source-specific-review-candidate — Review-required source-specific candidate

Same source-specific EID appears in two source-like exports without an accepted stable identifier, so Deduplication should require review.

Files:
- `bundles/FB03-source-specific-review-candidate/scopus_like_review_candidate_a.csv` — `scopus-csv`, source `scopus-like-public-metadata`, trace `trace-fb03-scopus-review-a`
- `bundles/FB03-source-specific-review-candidate/wos_like_source_specific_review_candidate_b.csv` — `scopus-csv`, source `wos-like-csv-fixture`, trace `trace-fb03-wos-review-b`

Expected coverage:

```json
{
  "importedRecords": 2,
  "sightings": 2,
  "parserWarningsAtLeast": 2,
  "dedupExactClusters": 0,
  "dedupReviewCandidatesAtLeast": 1,
  "suggestedBlockMode": "Review"
}
```

Notes:
- Parser warnings are expected because source-specific EID evidence is preserved but is not an approved stable WorkId namespace.

### FB04-warning-import — Controlled parser-warning and skipped-record import

Mutated RIS with bad year, missing title, and duplicate source record id to drive import warning summary blocks.

Files:
- `bundles/FB04-warning-import/wos_like_warning_mutated.ris` — `ris`, source `wos-like-mutated-public-metadata`, trace `trace-fb04-warning-ris`

Expected coverage:

```json
{
  "importedRecords": 4,
  "sightingsExpected": 2,
  "parserWarningsAtLeast": 3,
  "skippedRecordsAtLeast": 2,
  "dedupReviewCandidates": 0,
  "suggestedBlockMode": "Review"
}
```

Notes:
- One valid record has a non-integer year and should remain imported with a parser warning. Missing-title and duplicate-ID records should be skipped.

### FB05-noid-title-fuzzy-review — No-identifier title-only fuzzy review pair

Two title-only records with no DOI/arXiv/PMID/PMCID. They should remain unresolved and produce a review-required fuzzy-title candidate.

Files:
- `bundles/FB05-noid-title-fuzzy-review/google_scholar_style_title_only.bib` — `bibtex`, source `google-scholar-style-manual-bibtex`, trace `trace-fb05-scholar-title-only-a`
- `bundles/FB05-noid-title-fuzzy-review/openalex_like_title_only.ris` — `ris`, source `openalex-like-public-metadata`, trace `trace-fb05-openalex-title-only-b`

Expected coverage:

```json
{
  "importedRecords": 2,
  "sightings": 2,
  "parserWarnings": 0,
  "unresolvedCandidates": 2,
  "dedupExactClusters": 0,
  "dedupReviewCandidatesAtLeast": 1,
  "suggestedBlockMode": "Review"
}
```

Notes:
- The BibTeX is Google-Scholar-style only; it is not scraped or exported from Google Scholar.

### FB06-graph-identifier-preservation — Graph and source-specific identifier preservation

arXiv identifiers should become accepted WorkIds; SCI/WOS/ISBN-like fields should be preserved as source-specific evidence and warnings.

Files:
- `bundles/FB06-graph-identifier-preservation/openalex_like_graph_ids.ris` — `ris`, source `openalex-like-public-metadata`, trace `trace-fb06-openalex-graph`
- `bundles/FB06-graph-identifier-preservation/semantic_scholar_style_graph_ids.bib` — `bibtex`, source `semantic-scholar-style-public-metadata`, trace `trace-fb06-s2-graph`
- `bundles/FB06-graph-identifier-preservation/scopus_like_record_linkage.csv` — `scopus-csv`, source `scopus-like-public-metadata`, trace `trace-fb06-scopus-record-linkage`

Expected coverage:

```json
{
  "importedRecords": 4,
  "sightings": 4,
  "acceptedWorkIdNamespacesExpected": [
    "arxiv"
  ],
  "sourceSpecificWarningsExpected": true,
  "dedupReviewCandidates": 0,
  "suggestedBlockMode": "Review"
}
```

Notes:
- Review mode is expected if parser warnings are treated as warning conditions by APP-01.

### FB07-combined-app01-demo — Combined APP-01 demonstration

One bundle combining clean records, exact DOI duplicates, parser warnings/skipped records, source-specific review candidates, and no-id fuzzy review candidates.

Files:
- `bundles/FB07-combined-app01-demo/combined_scopus_like.csv` — `scopus-csv`, source `scopus-like-public-metadata`, trace `trace-fb07-combined-scopus`
- `bundles/FB07-combined-app01-demo/combined_wos_like.ris` — `ris`, source `wos-like-public-metadata`, trace `trace-fb07-combined-wos`
- `bundles/FB07-combined-app01-demo/combined_scholar_style.bib` — `bibtex`, source `google-scholar-style-manual-bibtex`, trace `trace-fb07-combined-scholar`
- `bundles/FB07-combined-app01-demo/combined_wos_like_source_specific.csv` — `scopus-csv`, source `wos-like-csv-fixture`, trace `trace-fb07-combined-wos-source-specific`

Expected coverage:

```json
{
  "importedRecordsAtLeast": 14,
  "skippedRecordsAtLeast": 2,
  "parserWarningsExpected": true,
  "dedupExactClustersAtLeast": 1,
  "dedupReviewCandidatesAtLeast": 2,
  "suggestedBlockMode": "Review"
}
```

Notes:
- This is the recommended first APP-01 smoke fixture after the composer is implemented.

## Recommended first APP-01 smoke path

Use `FB07-combined-app01-demo` after the APP-01 composer is implemented. Parse each file using its manifest `importRequest`, execute `DeduplicationService.Execute(...)`, then call `SearchDedupWorkspacePlanComposer.Compose(...)`.

Expected visible coverage:

- import summary with warnings;
- warning summary by category;
- exact DOI duplicate cluster for Rayyan;
- source-specific review candidate from shared EID;
- no-identifier fuzzy title review pair;
- human merge gate placeholder blocks;
- `BlockSourceKind.AppProjection` for every block.

## Suggested C# usage shape

```csharp
var importService = new SearchImportService();
var traces = new List<SearchImportTrace>();

foreach (var file in manifest.Bundles.Single(b => b.BundleId == "FB07-combined-app01-demo").Files)
{
    var request = new SearchImportRequest(
        file.ImportRequest.SourceDatabaseOrTool,
        file.ImportRequest.ExportFormat,
        file.ImportRequest.ParserId,
        file.ImportRequest.ParserVersion,
        file.ImportRequest.ImportedBy,
        file.ImportRequest.ImportedAt,
        file.ImportRequest.OriginalQueryText,
        file.ImportRequest.ExportedAt);

    var bytes = File.ReadAllBytes(Path.Combine(bundleRoot, file.Path));
    traces.Add(importService.Parse(file.TraceId, request, bytes));
}

var dedup = new DeduplicationService().Execute(
    "dedup-fb07-combined-app01-demo",
    Array.Empty<SearchTrace>(),
    traces);

var plan = new SearchDedupWorkspacePlanComposer().Compose(
    new SearchDedupWorkspacePlanInput(
        "workspace-fb07-combined-app01-demo",
        "APP-01 combined local projection demo",
        traces[0], // or use the accepted multi-trace input shape if PR3 broadens it
        dedup));
```

If APP-01 accepts exactly one `SearchImportTrace`, use `FB02`, `FB03`, `FB04`, or `FB05` independently. If PR3 supports multiple import traces, use `FB07` as the full demonstration.

## Local dry-run validation

A lightweight Python parser that mirrors the current import/dedup rules at fixture level produced `VALIDATION-DRY-RUN.json`. This is not a replacement for `dotnet test`; it is a format sanity check for this generated pack.

```json
[
  {
    "bundleId": "FB01-clean-single-source",
    "importedRecords": 4,
    "skippedRecords": 0,
    "sightings": 4,
    "parserWarnings": 0,
    "exactClusters": 0,
    "reviewCandidates": 0,
    "reviewReasons": []
  },
  {
    "bundleId": "FB02-cross-source-exact-duplicates",
    "importedRecords": 3,
    "skippedRecords": 0,
    "sightings": 3,
    "parserWarnings": 0,
    "exactClusters": 1,
    "reviewCandidates": 0,
    "reviewReasons": []
  },
  {
    "bundleId": "FB03-source-specific-review-candidate",
    "importedRecords": 2,
    "skippedRecords": 0,
    "sightings": 2,
    "parserWarnings": 2,
    "exactClusters": 0,
    "reviewCandidates": 1,
    "reviewReasons": [
      [
        "import:trace-fb03-scopus-review-a:1:2-s2.0-NEXUS-REVIEW-001",
        "import:trace-fb03-wos-review-b:1:2-s2.0-NEXUS-REVIEW-001",
        "source-specific",
        0.9853
      ]
    ]
  },
  {
    "bundleId": "FB04-warning-import",
    "importedRecords": 4,
    "skippedRecords": 2,
    "sightings": 2,
    "parserWarnings": 3,
    "exactClusters": 0,
    "reviewCandidates": 0,
    "reviewReasons": []
  },
  {
    "bundleId": "FB05-noid-title-fuzzy-review",
    "importedRecords": 2,
    "skippedRecords": 0,
    "sightings": 2,
    "parserWarnings": 0,
    "exactClusters": 0,
    "reviewCandidates": 1,
    "reviewReasons": [
      [
        "import:trace-fb05-scholar-title-only-a:1:LivingWorkbench2026A",
        "import:trace-fb05-openalex-title-only-b:1:OA-TITLE-ONLY-2026B",
        "fuzzy-title",
        1.0
      ]
    ]
  },
  {
    "bundleId": "FB06-graph-identifier-preservation",
    "importedRecords": 4,
    "skippedRecords": 0,
    "sightings": 4,
    "parserWarnings": 2,
    "exactClusters": 0,
    "reviewCandidates": 0,
    "reviewReasons": []
  },
  {
    "bundleId": "FB07-combined-app01-demo",
    "importedRecords": 14,
    "skippedRecords": 2,
    "sightings": 12,
    "parserWarnings": 5,
    "exactClusters": 1,
    "reviewCandidates": 4,
    "reviewReasons": [
      [
        "import:trace-fb07-combined-scopus:3:2-s2.0-NEXUS-REVIEW-001",
        "import:trace-fb07-combined-wos:3:WOS-COMBINED-REVIEW-B",
        "fuzzy-title",
        0.9853
      ],
      [
        "import:trace-fb07-combined-scopus:3:2-s2.0-NEXUS-REVIEW-001",
        "import:trace-fb07-combined-wos-source-specific:1:2-s2.0-NEXUS-REVIEW-001",
        "source-specific",
        0.9853
      ],
      [
        "import:trace-fb07-combined-scopus:4:row-4",
        "import:trace-fb07-combined-scholar:2:ScholarCombinedLivingWorkbenchB",
        "fuzzy-title",
        1.0
      ],
      [
        "import:trace-fb07-combined-wos:3:WOS-COMBINED-REVIEW-B",
        "import:trace-fb07-combined-wos-source-specific:1:2-s2.0-NEXUS-REVIEW-001",
        "fuzzy-title",
        1.0
      ]
    ]
  }
]
```
