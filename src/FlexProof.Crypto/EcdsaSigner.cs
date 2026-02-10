using System.Security.Cryptography;

namespace FlexProof.Crypto;

/// <summary>
/// ECDSA P-256 signer for producing detached signatures.
/// Wraps a single key pair; for key rotation, create a new instance with a new key.
/// </summary>
public sealed class EcdsaSigner : IArtifactSigner, IDisposable
{
    private readonly ECDsa _key;

    /// <summary>Create a signer with a new random key pair.</summary>
    public EcdsaSigner()
    {
        _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    /// <summary>Create a signer from an existing private key (PKCS#8 format).</summary>
    public EcdsaSigner(byte[] pkcs8PrivateKey)
    {
        _key = ECDsa.Create();
        _key.ImportPkcs8PrivateKey(pkcs8PrivateKey, out _);
    }

    public byte[] Sign(byte[] data)
    {
        return _key.SignData(data, HashAlgorithmName.SHA256);
    }

    public byte[] GetPublicKey()
    {
        return _key.ExportSubjectPublicKeyInfo();
    }

    /// <summary>Export the private key in PKCS#8 format for secure storage.</summary>
    public byte[] ExportPrivateKey()
    {
        return _key.ExportPkcs8PrivateKey();
    }

    public void Dispose()
    {
        _key.Dispose();
    }
}
