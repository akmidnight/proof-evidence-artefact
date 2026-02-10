using FlexProof.Domain;

namespace FlexProof.Crypto;

/// <summary>
/// Produces a SHA-256 commitment over the canonicalized claim inputs.
/// </summary>
public interface IArtifactCommitter
{
    /// <summary>
    /// Computes a deterministic SHA-256 hex digest for the given artifact's claim inputs.
    /// </summary>
    string ComputeCommitment(UsageRightArtifact artifact);
}
