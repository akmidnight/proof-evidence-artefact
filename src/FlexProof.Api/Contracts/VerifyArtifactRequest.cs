using FlexProof.Domain;

namespace FlexProof.Api.Contracts;

/// <summary>
/// Request to verify an existing artifact.
/// Accepts either an artifact ID (looked up from registry) or a full artifact payload.
/// </summary>
public sealed class VerifyArtifactRequest
{
    /// <summary>ID of an artifact already stored in the registry.</summary>
    public string? ArtifactId { get; init; }

    /// <summary>Full artifact payload for standalone verification.</summary>
    public UsageRightArtifact? Artifact { get; init; }
}
