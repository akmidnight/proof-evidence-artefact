namespace FlexProof.Adapter.Local;

/// <summary>
/// A single aggregated load reading (e.g. 15-minute average).
/// This is the minimal ingestion unit -- never raw session-level data.
/// </summary>
public sealed class LoadReading
{
    /// <summary>Start of the measurement interval.</summary>
    public required DateTimeOffset IntervalStart { get; init; }

    /// <summary>Duration of the measurement interval.</summary>
    public required TimeSpan IntervalDuration { get; init; }

    /// <summary>Average power during the interval in kW.</summary>
    public required double AverageKw { get; init; }

    /// <summary>Energy delivered during the interval in kWh.</summary>
    public required double EnergyKwh { get; init; }
}
