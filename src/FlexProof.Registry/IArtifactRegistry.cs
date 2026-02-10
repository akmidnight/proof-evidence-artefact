using FlexProof.Domain;

namespace FlexProof.Registry;

/// <summary>
/// Append-only artifact registry with audit trail.
/// </summary>
public interface IArtifactRegistry
{
    /// <summary>Store a newly issued artifact and record the issuance event.</summary>
    Task StoreAsync(UsageRightArtifact artifact, string actorId, CancellationToken ct = default);

    /// <summary>Retrieve an artifact by its ID.</summary>
    Task<UsageRightArtifact?> GetAsync(string artifactId, CancellationToken ct = default);

    /// <summary>List all stored artifacts.</summary>
    Task<IReadOnlyList<UsageRightArtifact>> ListAsync(CancellationToken ct = default);

    /// <summary>Mark an artifact as revoked.</summary>
    Task RevokeAsync(string artifactId, string reason, string actorId, CancellationToken ct = default);

    /// <summary>Supersede an artifact with a new one.</summary>
    Task SupersedeAsync(string oldArtifactId, UsageRightArtifact replacement, string actorId, CancellationToken ct = default);

    /// <summary>Record a verification event in the audit log.</summary>
    Task RecordVerificationAsync(string artifactId, VerificationResult result, string actorId, CancellationToken ct = default);

    /// <summary>Get the audit trail for a given artifact.</summary>
    Task<IReadOnlyList<AuditEntry>> GetAuditTrailAsync(string artifactId, CancellationToken ct = default);
}
