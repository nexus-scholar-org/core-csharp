# FE-09A Retained Crossref Fixtures

Status: local conformance evidence only.

These files are exact retained UTF-8 response bytes for ADR 0039 tests. They are
hand-authored local fixtures informed by the pinned PHP Crossref cassette shape
and current official Crossref REST documentation. They are not generated PHP
goldens, live-response snapshots, or Crossref compatibility claims.

| File | SHA-256 over exact bytes | Purpose |
| --- | --- | --- |
| `search-crossref-recorded-page.response.json` | `2405ac529ce4efcf126b7188ac39bd5ef86e75334cf624f8e196f8e744377120` | Successful work-list page with duplicate DOI sightings and a next offset |
| `search-crossref-rate-limit-response.response.json` | `e04383bb404c2a90e0e6f02266181c16e5eef0ffd9cbea1a1f50a401f2c19cc4` | Recorded `429` body used with caller-supplied retry/rate-limit headers |
| `search-crossref-schema-drift.response.json` | `0c9cb6c198a10acbfb889624cc7a0b2719edb7791afde785ad62377ac41268aa` | Unsupported response schema with retained response evidence |
| `search-crossref-partial-page.response.json` | `f22d6d58da8c98e93924efcfe7a3ca7d1ba13ceef5c05dcbb0b5d2ee82f4ae02` | Short page before the declared total |
| `search-crossref-no-doi.response.json` | `eaedf15c0a07845acb241db2713491921433de67956ad908d7580bd60ac34561` | Missing DOI preserved as an unresolved sighting |
| `search-crossref-empty-page.response.json` | `737cf1686c8fa223775ab809aadcd3483124544057e391357b0b16c4574af284` | Empty non-final page preserved as partial |
| `search-crossref-page-window-drift.response.json` | `dec11aec6bce3547478783897add5fad4432fa67bf1e3abf4b7d146e4591f8f9` | Response start index disagrees with the bound page request |
| `search-crossref-malformed.response.json` | `a6fb08fda1acb957b6116bd37811a1fe41a01611c0631edbf786d6889a27a55c` | Malformed JSON with retained response evidence |

The focused test project mutates one retained response in memory solely to prove
that declared digest verification fails. All parse inputs otherwise come from
the retained files above and are accepted through
`RecordedProviderFixtureEvidence` before parsing.

No fixture authorizes networking, credentials, identified polite-pool access,
runtime retention, retries, throttling, caching, or production provider use.
