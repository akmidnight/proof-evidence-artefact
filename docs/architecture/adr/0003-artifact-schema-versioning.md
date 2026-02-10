# ADR-0003: Artifact Schema Versioning

## Status

Accepted

## Context

As the system evolves, new claim types and artifact fields will be added.
Counterparties relying on published artifacts need stability guarantees.

## Decision

1. Every artifact carries a `schemaVersion` field (currently `1.0`).
2. Schema changes follow semantic versioning:
   - **Patch** (1.0.x): documentation clarifications, no data changes.
   - **Minor** (1.x.0): additive changes (new optional fields, new claim types).
     Existing verifiers continue to work.
   - **Major** (x.0.0): breaking changes (removed/renamed fields, changed
     commitment algorithm). Requires verifier updates.
3. The JSON Schema in `docs/specs/usage-right-artifact.schema.json` is the
   canonical definition. API responses must validate against it.
4. Major version bumps require a migration guide and a deprecation period of
   at least one minor release.

## Consequences

- **Positive**: counterparties can rely on stable artifact formats; clear
  upgrade path.
- **Negative**: major version bumps require coordination across all parties.
