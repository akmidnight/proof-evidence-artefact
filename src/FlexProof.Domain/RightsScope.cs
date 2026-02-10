namespace FlexProof.Domain;

/// <summary>
/// Defines the purpose-bound rights and constraints attached to an artifact.
/// Ensures artifacts are scoped to a specific use, counterparty, and time range.
/// </summary>
public sealed class RightsScope
{
    /// <summary>Identifier of the counterparty allowed to consume this artifact.</summary>
    public required string CounterpartyId { get; init; }

    /// <summary>Human-readable purpose description (e.g. "tariff negotiation").</summary>
    public required string Purpose { get; init; }

    /// <summary>Start of the validity window for the rights grant.</summary>
    public required DateTimeOffset ValidFrom { get; init; }

    /// <summary>End of the validity window for the rights grant.</summary>
    public required DateTimeOffset ValidTo { get; init; }

    /// <summary>Optional additional constraints expressed as key-value pairs.</summary>
    public Dictionary<string, string> Constraints { get; init; } = new();
}
