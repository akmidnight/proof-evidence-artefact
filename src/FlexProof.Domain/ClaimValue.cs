namespace FlexProof.Domain;

/// <summary>
/// The quantitative claim being asserted by an artifact.
/// </summary>
public sealed class ClaimValue
{
    /// <summary>Type of claim (peak compliance, demand delta, etc.).</summary>
    public required ClaimType Type { get; init; }

    /// <summary>
    /// Metric name (e.g. "peak_kw", "demand_charge_delta_pct").
    /// </summary>
    public required string MetricName { get; init; }

    /// <summary>The numeric or categorical value of the claim.</summary>
    public required double Value { get; init; }

    /// <summary>Unit of measurement (e.g. "kW", "%").</summary>
    public required string Unit { get; init; }

    /// <summary>Reference to the baseline computation used, if applicable.</summary>
    public string? BaselineRef { get; init; }

    /// <summary>Version tag of the computation logic that produced this claim.</summary>
    public required string ComputationVersion { get; init; }
}
