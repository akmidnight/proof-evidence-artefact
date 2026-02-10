namespace FlexProof.Api.Contracts;

/// <summary>
/// Request to revoke an issued artifact.
/// </summary>
public sealed class RevokeArtifactRequest
{
    /// <summary>ID of the artifact to revoke.</summary>
    public required string ArtifactId { get; init; }

    /// <summary>Reason for revocation.</summary>
    public required string Reason { get; init; }
}
