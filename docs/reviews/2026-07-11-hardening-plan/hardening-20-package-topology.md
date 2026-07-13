# Hardening 20 - Package Topology And Clean Install

Status: complete.

## Delivered

- accepted ADR 0025 and a machine-readable twelve-package domain allowlist;
- kept six operational/UI projects, all samples, previews, and tests non-packable;
- added common `0.1.0-alpha.1`, MIT, repository, tags, README, and early-alpha metadata;
- embedded repository README and LICENSE in every package;
- added double-pack normalized-content reproducibility checks;
- added raw and normalized SHA-256 package manifests;
- added a local-source-only clean restore and runtime assembly-load smoke application;
- added package validation to local verification and hosted Windows/Linux CI.

## Verification

- package topology: exactly 12 packages, no extras or omissions;
- metadata: version, MIT license, README, and LICENSE verified in every archive;
- normalized reproducibility: two independent packs matched for all 12 packages;
- clean smoke: four leaf packages restored the complete graph and loaded all 12 assemblies;
- publication: disabled.

## ADR And Compatibility Impact

ADR 0025 is accepted. No scientific behavior, PHP fixture, or compatibility claim changed.
