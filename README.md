# FlexProof - Verifiable Usage-Right Artifacts for EV Charging Infrastructure

FlexProof is a neutral evidence layer that converts EV charging optimization outcomes into **verifiable, investable, and monetizable usage-right artifacts** -- without transferring raw operational data.

## Why

Grid operators, fleet managers, and investors need **proof** that charging infrastructure behaves as claimed. But no party wants raw-data liability. FlexProof bridges this gap by issuing cryptographically committed, purpose-bound artifacts that third parties can independently verify.

## Architecture

```
Depot Systems --> Local Adapter --> Artifact Engine --> Commitment & Signing
                                                           |
                                            Artifact Registry (append-only)
                                              |             |
                                       Verifier API     Audit Log
                                              |
                                     Counterparty Portal
```

### Roles

| Role | Responsibility |
|------|----------------|
| **Flexecharge** | Operates and optimizes charging infrastructure |
| **Data owners** (fleet / CPO) | Retain data sovereignty |
| **Counterparties** (DSO, investors) | Consume evidence, not data |
| **FlexProof** (this layer) | Verify and attest outcomes ex-post |

### Key Principles

- Raw data stays local -- artifacts carry commitments, not payloads
- Every artifact has a defined purpose, bounded timeframe, named counterparty
- Verification is deterministic and reproducible
- Append-only audit trail for every lifecycle event

## Projects

| Project | Description |
|---------|-------------|
| `FlexProof.Domain` | Core models (Artifact, Claim, RightsScope, lifecycle states) |
| `FlexProof.Crypto` | SHA-256 commitments, ECDSA P-256 signing, verification |
| `FlexProof.ArtifactEngine` | Claims generation from local aggregated summaries |
| `FlexProof.Registry` | Append-only artifact persistence and audit trail |
| `FlexProof.Api` | REST API for issuance, verification, and audit |
| `FlexProof.Adapter.Local` | Data-source connectors and local normalization |
| `FlexProof.Ui` | Angular portal for operators and counterparties |

## Quick Start

### Prerequisites

- .NET 10 SDK
- Node.js 20+ and npm

### Build and Run

```bash
# Restore and build .NET solution
dotnet build FlexProof.slnx

# Run the API
dotnet run --project src/FlexProof.Api

# Install Angular dependencies and serve UI
cd src/FlexProof.Ui
npm install
npm start
```

### Run with Docker

```bash
docker compose up --build
```

### Run Tests

```bash
dotnet test FlexProof.slnx
```

## Pilot Focus: Tariff Negotiation

The initial pilot demonstrates artifacts for fleet tariff negotiation:

1. **Peak Window Compliance** -- prove peak load stayed below contractual thresholds
2. **Demand Charge Delta** -- prove controlled charging reduced demand charge exposure vs baseline

Success signal: a counterparty accepts the artifact instead of requesting raw operational data.

## License

Apache-2.0. See [LICENSE](LICENSE).
