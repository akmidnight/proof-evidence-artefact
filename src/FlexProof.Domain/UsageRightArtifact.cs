namespace FlexProof.Domain;

/// <summary>
/// A verifiable, purpose-bound usage-right artifact.
/// Represents a cryptographically committed claim about an operational outcome,
/// scoped to a specific counterparty, purpose, and timeframe.
/// </summary>
public sealed class UsageRightArtifact
{
    /// <summary>Globally unique artifact identifier.</summary>
    public required string ArtifactId { get; init; }

    /// <summary>Version of the artifact schema.</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Identifier of the issuing entity.</summary>
    public required string IssuerId { get; init; }

    /// <summary>Timestamp when the artifact was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Current lifecycle state.</summary>
    public ArtifactState State { get; set; } = ArtifactState.Draft;

    /// <summary>Start of the observation period the claim covers.</summary>
    public required DateTimeOffset PeriodStart { get; init; }

    /// <summary>End of the observation period the claim covers.</summary>
    public required DateTimeOffset PeriodEnd { get; init; }

    /// <summary>The quantitative claim asserted by this artifact.</summary>
    public required ClaimValue Claim { get; init; }

    /// <summary>Purpose-bound rights and constraints.</summary>
    public required RightsScope Rights { get; init; }

    /// <summary>
    /// SHA-256 hex digest computed over the canonicalized claim inputs.
    /// Set during the commitment step.
    /// </summary>
    public string? DataCommitment { get; set; }

    /// <summary>
    /// Base64-encoded ECDSA P-256 detached signature over the canonical representation.
    /// Set during the signing step.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Base64-encoded ECDSA P-256 public key (SPKI format) used for signing.
    /// </summary>
    public string? SignerPublicKey { get; set; }

    /// <summary>
    /// Reference to a superseding artifact, if this one was superseded.
    /// </summary>
    public string? SupersededBy { get; set; }

    /// <summary>
    /// Reference to the revocation record, if revoked.
    /// </summary>
    public string? RevocationRef { get; set; }
}
