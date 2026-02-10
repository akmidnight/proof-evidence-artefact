# Threat Model

## Scope

This document covers threats to the FlexProof evidence layer, focusing on
artifact integrity, authenticity, and data minimization.

## Assets

1. **Issued artifacts** -- must be tamper-evident and verifiable.
2. **Private signing keys** -- must remain under operator custody.
3. **Raw operational data** -- must never leave the local adapter boundary.
4. **Audit trail** -- must be append-only and non-repudiable.

## Threats and Mitigations

### T1: Forged Claims

**Threat**: An attacker creates an artifact with a false claim value.

**Mitigation**: The SHA-256 commitment over canonicalized inputs means any
change to claim fields invalidates the commitment. The ECDSA signature ties
the commitment to a specific key pair. Verifiers re-compute the commitment
and check the signature independently.

**Test**: `ThreatModelTests.ForgedClaim_CommitmentMismatch_FailsVerification`

### T2: Replay Attacks

**Threat**: An attacker copies a valid artifact's commitment and signature
to a different artifact (e.g. with a different artifact ID or period).

**Mitigation**: The artifact ID and all claim-relevant fields are included
in the canonicalized input. Changing any field (including the ID) breaks the
commitment match.

**Test**: `ThreatModelTests.ReplayAttack_DifferentArtifactId_CommitmentMismatch`

### T3: Key Misuse / Impersonation

**Threat**: An attacker signs with their own key but presents a different
entity's public key.

**Mitigation**: The public key embedded in the artifact is the one used for
verification. If it doesn't match the actual signing key, the signature check
fails. In production, public keys should be registered in a trusted key
directory or certificate chain.

**Test**: `ThreatModelTests.KeyMisuse_WrongPublicKey_SignatureInvalid`

### T4: Raw Data Exfiltration

**Threat**: API endpoints or UI exports leak raw session-level data.

**Mitigation**: The adapter layer enforces aggregation. API contract tests
verify that response shapes contain only artifact-level fields. No endpoints
return raw load readings.

**Enforcement**: Data minimization policy (ADR-0002) and code-level boundary
enforcement in the adapter interface.

### T5: Audit Log Tampering

**Threat**: An operator deletes or modifies audit entries to hide revocations.

**Mitigation**: The registry is append-only. The `InMemoryArtifactRegistry`
uses a `ConcurrentBag` that does not support deletion. In production, an
immutable event store or blockchain-anchored log should be used.

### T6: Expired or Overly Broad Rights

**Threat**: A counterparty uses an artifact outside its rights window or
for a purpose not covered by the rights scope.

**Mitigation**: Verification checks the current time against the rights
validity window. Purpose and counterparty are explicit fields that verifiers
should match against their own identity.

**Test**: `ThreatModelTests.ExpiredRights_FailsVerification`

## Residual Risks

- Key custody is local; physical compromise of the signing machine is outside
  this system's control.
- The in-memory registry is not durable across restarts. Production deployments
  must use persistent, append-only storage.
- The counterfactual baseline model is a simplification; disputes about baseline
  methodology are a business risk mitigated by transparent, versioned computation
  logic.
