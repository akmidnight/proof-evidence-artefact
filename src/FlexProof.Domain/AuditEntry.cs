namespace FlexProof.Domain;

/// <summary>
/// An immutable audit log entry recording a lifecycle event for an artifact.
/// </summary>
public sealed class AuditEntry
{
    /// <summary>Unique identifier for this audit entry.</summary>
    public required string EntryId { get; init; }

    /// <summary>The artifact this entry relates to.</summary>
    public required string ArtifactId { get; init; }

    /// <summary>Timestamp of the event.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Type of event (e.g. "Created", "Issued", "Verified", "Revoked").</summary>
    public required string EventType { get; init; }

    /// <summary>Identity of the actor that triggered the event.</summary>
    public required string ActorId { get; init; }

    /// <summary>Optional human-readable detail about the event.</summary>
    public string? Detail { get; init; }
}
