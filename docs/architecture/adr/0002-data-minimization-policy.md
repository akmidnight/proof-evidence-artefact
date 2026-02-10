# ADR-0002: Data Minimization Policy

## Status

Accepted

## Context

The evidence layer must prevent accidental or intentional exposure of raw
operational data. Multiple stakeholders (fleet operators, DSOs, investors)
interact with the system, and none should receive more data than necessary
for their role.

## Decision

1. The local adapter layer enforces data minimization at the source boundary.
   Only aggregated scalar values (e.g. 15-minute interval averages) are passed
   to the artifact engine. Raw session data (vehicle IDs, driver info, per-plug
   telemetry) never crosses this boundary.

2. API responses and UI exports contain only artifact metadata, claims, and
   verification results. No raw data endpoints exist.

3. The canonicalized input for cryptographic commitments contains only the
   minimum fields needed for deterministic verification (artifact ID, issuer,
   period, claim, rights scope).

4. Integration tests enforce that API contract shapes contain no raw data fields.

## Consequences

- **Positive**: strong privacy posture, reduced regulatory exposure, clear
  liability boundaries.
- **Negative**: debugging requires access to the local adapter layer; remote
  troubleshooting is limited by design.
