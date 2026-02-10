using FlexProof.Domain;

namespace FlexProof.Crypto;

/// <summary>
/// Verifies the integrity and authenticity of a usage-right artifact.
/// </summary>
public interface IArtifactVerifier
{
    /// <summary>
    /// Runs all verification checks on the artifact and returns a deterministic result.
    /// </summary>
    VerificationResult Verify(UsageRightArtifact artifact);
}
