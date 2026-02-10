namespace FlexProof.Crypto;

/// <summary>
/// Signs data using ECDSA P-256 and provides key management operations.
/// </summary>
public interface IArtifactSigner
{
    /// <summary>Produce a detached ECDSA P-256 signature over the given data bytes.</summary>
    byte[] Sign(byte[] data);

    /// <summary>Get the current public key as a byte array.</summary>
    byte[] GetPublicKey();
}
