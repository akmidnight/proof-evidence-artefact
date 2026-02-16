# Changelog

All notable changes to FlexProof will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [0.1.0] - 2026-02-10

### Added

- Core domain model: `UsageRightArtifact`, `ClaimValue`, `RightsScope`, `AuditEntry`, `VerificationResult`.
- Artifact lifecycle states: Draft, Issued, Revoked, Superseded.
- Two tariff pilot claim types: `PeakWindowCompliance`, `DemandChargeDeltaEstimate`.
- SHA-256 commitment generation over canonicalized claim inputs.
- ECDSA P-256 detached signatures with PKCS#8 key export/import.
- Deterministic artifact verification with explicit pass/fail reason codes.
- Local adapter layer with data minimization enforcement.
- Baseline engine (historical lookback and counterfactual model).
- Append-only in-memory artifact registry with audit trail.
- REST API: issue, verify, get, list, revoke, audit trail endpoints.
- Angular UI: artifact list, detail, issuance form, verification page.
- Download verification report and artifact JSON from UI.
- Pilot runner generating 5 tariff artifacts across 2 depots.
- Counterparty acceptance packet (artifacts, verifications, audit trail, summary).
- Threat model with 7 abuse-case tests (forged claims, replay, key misuse, etc.).
- Data minimization enforcement tests (7 tests verifying no raw data in API responses).
- Structured JSON logging for observability without sensitive payload leakage.
- CODEOWNERS file for pull request review routing.
- CI workflow (GitHub Actions) for .NET and Angular builds.
- Docker and docker-compose for local containerized deployment.
- Architecture docs: roles and trust model, ADRs, key custody playbook.
- JSON Schema for artifact validation.
- Apache-2.0 license.
