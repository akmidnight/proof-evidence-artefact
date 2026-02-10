namespace FlexProof.Domain;

/// <summary>
/// Lifecycle states of a usage-right artifact.
/// </summary>
public enum ArtifactState
{
    Draft,
    Issued,
    Revoked,
    Superseded
}
