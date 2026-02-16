using FlexProof.Domain;

namespace FlexProof.Adapter.Local;

/// <summary>
/// Aggregation result ready for claim generation.
/// Contains only minimized, aggregated values -- never raw session data.
/// </summary>
public sealed class AggregatedClaimInput
{
    public required ClaimType ClaimType { get; init; }
    public required double Value { get; init; }
    public required string Unit { get; init; }
    public required string MetricName { get; init; }
    public required DateTimeOffset PeriodStart { get; init; }
    public required DateTimeOffset PeriodEnd { get; init; }
    public string? BaselineRef { get; init; }
}

/// <summary>
/// Produces aggregated claim inputs from local data.
/// Enforces data minimization: only aggregated scalars leave this layer.
/// </summary>
public sealed class ClaimInputAggregator
{
    private readonly BaselineEngine _baseline = new();

    /// <summary>
    /// Computes a peak-window compliance claim input.
    /// Returns the maximum observed load during tariff peak windows.
    /// </summary>
    public AggregatedClaimInput AggregatePeakCompliance(
        IReadOnlyList<LoadReading> readings,
        IReadOnlyList<TariffWindow> tariffWindows,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        var peakReadings = readings
            .Where(r => IsInPeakWindow(r, tariffWindows))
            .ToList();

        var peakKw = peakReadings.Count > 0 ? peakReadings.Max(r => r.AverageKw) : 0;

        return new AggregatedClaimInput
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            Value = Math.Round(peakKw, 2),
            Unit = "kW",
            MetricName = "peak_kw",
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        };
    }

    /// <summary>
    /// Computes a demand-charge delta claim input.
    /// Returns the percentage reduction vs baseline.
    /// </summary>
    public AggregatedClaimInput AggregateDemandDelta(
        IReadOnlyList<LoadReading> actualReadings,
        IReadOnlyList<LoadReading> lookbackReadings,
        IReadOnlyList<TariffWindow> tariffWindows,
        BaselineMode mode,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        double baselinePeak = mode switch
        {
            BaselineMode.HistoricalLookback =>
                _baseline.ComputeHistoricalBaseline(lookbackReadings, tariffWindows),
            BaselineMode.CounterfactualModel =>
                _baseline.ComputeCounterfactualBaseline(actualReadings),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        var actualPeakReadings = actualReadings
            .Where(r => IsInPeakWindow(r, tariffWindows))
            .ToList();
        var actualPeak = actualPeakReadings.Count > 0 ? actualPeakReadings.Max(r => r.AverageKw) : 0;

        var deltaPct = baselinePeak > 0
            ? Math.Round((baselinePeak - actualPeak) / baselinePeak * 100, 2)
            : 0;

        var baselineRef = mode switch
        {
            BaselineMode.HistoricalLookback => "lookback-30d-v1",
            BaselineMode.CounterfactualModel => "counterfactual-v1",
            _ => "unknown"
        };

        return new AggregatedClaimInput
        {
            ClaimType = ClaimType.DemandChargeDeltaEstimate,
            Value = deltaPct,
            Unit = "%",
            MetricName = "demand_charge_delta_pct",
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            BaselineRef = baselineRef
        };
    }

    private static bool IsInPeakWindow(LoadReading reading, IReadOnlyList<TariffWindow> windows)
    {
        var time = TimeOnly.FromDateTime(reading.IntervalStart.LocalDateTime);
        var day = reading.IntervalStart.DayOfWeek;
        return windows.Any(w =>
            w.ApplicableDays.Contains(day) &&
            time >= w.StartTime &&
            time < w.EndTime);
    }
}
