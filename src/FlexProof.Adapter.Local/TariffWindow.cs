namespace FlexProof.Adapter.Local;

/// <summary>
/// Defines a time-of-use tariff window during which peak behavior matters.
/// </summary>
public sealed class TariffWindow
{
    /// <summary>Human label (e.g. "Peak", "Off-Peak").</summary>
    public required string Label { get; init; }

    /// <summary>Start time of day for this window.</summary>
    public required TimeOnly StartTime { get; init; }

    /// <summary>End time of day for this window.</summary>
    public required TimeOnly EndTime { get; init; }

    /// <summary>Days of the week this window applies to.</summary>
    public required DayOfWeek[] ApplicableDays { get; init; }
}
