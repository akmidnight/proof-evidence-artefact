using FlexProof.Adapter.Local;
using FlexProof.Domain;

namespace FlexProof.Api.Contracts;

/// <summary>
/// Request to issue a new usage-right artifact.
/// </summary>
public sealed class IssueArtifactRequest
{
    /// <summary>Claim type to generate.</summary>
    public required ClaimType ClaimType { get; init; }

    /// <summary>Start of the observation period.</summary>
    public required DateTimeOffset PeriodStart { get; init; }

    /// <summary>End of the observation period.</summary>
    public required DateTimeOffset PeriodEnd { get; init; }

    /// <summary>Baseline mode for delta claims. Ignored for compliance claims.</summary>
    public BaselineMode BaselineMode { get; init; } = BaselineMode.HistoricalLookback;

    /// <summary>Start of the lookback period for historical baseline. Ignored if not applicable.</summary>
    public DateTimeOffset? LookbackStart { get; init; }

    /// <summary>Counterparty identifier.</summary>
    public required string CounterpartyId { get; init; }

    /// <summary>Purpose description.</summary>
    public required string Purpose { get; init; }

    /// <summary>Rights validity start.</summary>
    public required DateTimeOffset RightsValidFrom { get; init; }

    /// <summary>Rights validity end.</summary>
    public required DateTimeOffset RightsValidTo { get; init; }
}
