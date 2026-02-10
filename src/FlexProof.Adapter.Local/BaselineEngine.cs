namespace FlexProof.Adapter.Local;

/// <summary>
/// Computes baseline demand values used for delta/comparison claims.
/// </summary>
public sealed class BaselineEngine
{
    /// <summary>
    /// Computes baseline peak demand from historical readings using
    /// average of maximum interval readings per day in the lookback period.
    /// </summary>
    public double ComputeHistoricalBaseline(
        IReadOnlyList<LoadReading> lookbackReadings,
        IReadOnlyList<TariffWindow> tariffWindows)
    {
        if (lookbackReadings.Count == 0)
            return 0;

        // Group readings by date, filter to those falling within tariff peak windows,
        // then take the max per day and average across all days.
        var dailyPeaks = lookbackReadings
            .Where(r => IsInPeakWindow(r, tariffWindows))
            .GroupBy(r => r.IntervalStart.Date)
            .Select(g => g.Max(r => r.AverageKw))
            .ToList();

        return dailyPeaks.Count > 0 ? dailyPeaks.Average() : 0;
    }

    /// <summary>
    /// Computes baseline peak demand using a counterfactual model.
    /// Assumes uniform energy distribution across session durations without optimization.
    /// </summary>
    public double ComputeCounterfactualBaseline(
        IReadOnlyList<LoadReading> actualReadings,
        double simultaneityFactor = 1.0)
    {
        if (actualReadings.Count == 0)
            return 0;

        // Simple counterfactual: total energy distributed uniformly implies
        // higher coincident peak by the simultaneity factor.
        var totalEnergy = actualReadings.Sum(r => r.EnergyKwh);
        var totalHours = actualReadings.Sum(r => r.IntervalDuration.TotalHours);
        var averagePower = totalHours > 0 ? totalEnergy / totalHours : 0;

        return averagePower * simultaneityFactor;
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
