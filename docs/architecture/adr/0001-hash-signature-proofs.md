# ADR-0001: Use Hash Commitments and Digital Signatures for Phase 1

## Status

Accepted

## Context

The evidence layer needs a proof mechanism that is auditable, reproducible, and
acceptable to counterparties. Options considered:

1. **Hash commitments + digital signatures** -- straightforward, audit-friendly,
   well-understood by enterprise counterparties.
2. **ZK-ready architecture without live ZK circuits** -- adds complexity without
   immediate benefit for the tariff-negotiation pilot.
3. **Full ZK proofs** -- highest privacy guarantees but significant implementation
   effort and counterparty education overhead.

## Decision

Phase 1 uses SHA-256 hash commitments over canonicalized claim inputs combined
with ECDSA P-256 detached digital signatures. ECDSA P-256 was chosen over Ed25519
for broader ecosystem compatibility and mature .NET SDK support. The architecture
is designed so that ZK circuits can be introduced later as an alternative
commitment strategy.

## Consequences

- **Positive**: fast to implement, easy to explain to counterparties, deterministic
  verification, well-supported by standard libraries.
- **Negative**: the commitment reveals the hash of the input (not a concern for
  aggregated claims, but limits future privacy-sensitive use cases).
- **Mitigation**: the adapter layer enforces data minimization so that only
  aggregated values enter the commitment, not raw session data.
