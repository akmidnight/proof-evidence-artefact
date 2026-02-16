# Roles and Trust Model

## Overview

FlexProof enforces a clean separation of roles to prevent liability creep and
ensure each party interacts only with the evidence layer appropriate to its
function.

## Roles

### Operator (Flexecharge)

- Runs the charging optimization stack (load management, peak shaving, scheduling).
- Produces operational results that the evidence layer consumes in aggregate form.
- Does **not** become a data custodian or market intermediary.

### Data Owner (Fleet Operator / CPO)

- Retains full sovereignty over raw operational data.
- Authorises the local adapter to produce aggregated claim inputs.
- Raw data never leaves the data owner's environment.

### Counterparty (DSO / Investor / Insurer)

- Consumes verifiable artifacts, **not** raw data.
- Can independently verify artifact integrity and claim validity.
- Uses artifacts for tariff decisions, financing, or risk assessment.

### Evidence Layer (FlexProof)

- Issues and attests usage-right artifacts ex-post.
- Performs cryptographic commitment and signing.
- Maintains an append-only audit trail of all lifecycle events.
- Does **not** store or forward raw operational data.

## Trust Boundaries

```
  Data Owner Environment        Evidence Layer          Counterparty
  +---------------------+    +------------------+    +----------------+
  | Raw Data            |    | Artifact Engine  |    | Verify API     |
  | Local Adapter  -----+--->| Commitment       |--->| Artifact       |
  | (aggregation only)  |    | Signing          |    | Inspection     |
  +---------------------+    | Registry (audit) |    +----------------+
                              +------------------+
```

- The boundary between Data Owner and Evidence Layer enforces data minimization.
- The boundary between Evidence Layer and Counterparty enforces purpose limitation.

## Artifact Lifecycle States

| State | Description |
|-------|-------------|
| `Draft` | Artifact created but not yet signed/committed |
| `Issued` | Signed, committed, and available for verification |
| `Revoked` | Withdrawn due to error or policy change |
| `Superseded` | Replaced by a corrected artifact |
