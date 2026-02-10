# Key Custody Playbook

## Overview

FlexProof uses ECDSA P-256 key pairs for signing artifacts. Keys are generated
and stored locally -- never transmitted to a remote service.

## Key Lifecycle

### Generation

```bash
# Keys are generated automatically when the signer is instantiated.
# For explicit key generation, use the EcdsaSigner class:
#   var signer = new EcdsaSigner();
#   var privateKey = signer.ExportPrivateKey();
```

Store the exported PKCS#8 private key in a secure location:
- Hardware Security Module (HSM) for production
- Encrypted file with restricted ACLs for pilot

### Rotation

1. Generate a new key pair.
2. Issue new artifacts with the new key. Old artifacts remain verifiable
   with the old public key.
3. Publish the new public key to counterparties.
4. After a transition period, revoke the old key.

### Revocation

If a private key is compromised:
1. Stop issuing artifacts with the compromised key immediately.
2. Revoke all artifacts signed with the compromised key.
3. Re-issue corrected artifacts with a new key (supersession flow).
4. Notify all counterparties.

### Storage Recommendations

| Environment | Storage | Access Control |
|-------------|---------|----------------|
| Development | Encrypted file on disk | Developer-only ACL |
| Pilot | Encrypted file or software keystore | Operator-only ACL |
| Production | HSM (PKCS#11) or managed KMS | Role-based access |

## Public Key Distribution

Counterparties need the public key to verify artifacts. Options:
- Include `signerPublicKey` in the artifact envelope (current approach).
- Publish a key directory / certificate signed by a root CA (future).
- Use a well-known endpoint (`/.well-known/flexproof-keys.json`).
