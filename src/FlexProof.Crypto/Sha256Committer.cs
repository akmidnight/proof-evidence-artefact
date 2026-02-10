using System.Security.Cryptography;
using FlexProof.Domain;

namespace FlexProof.Crypto;

/// <summary>
/// Computes SHA-256 commitments over canonicalized artifact claim inputs.
/// </summary>
public sealed class Sha256Committer : IArtifactCommitter
{
    public string ComputeCommitment(UsageRightArtifact artifact)
    {
        var canonical = Canonicalizer.Canonicalize(artifact);
        var hash = SHA256.HashData(canonical);
        return Convert.ToHexStringLower(hash);
    }
}
