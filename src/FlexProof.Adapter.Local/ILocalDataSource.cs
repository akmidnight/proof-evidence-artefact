namespace FlexProof.Adapter.Local;

/// <summary>
/// Interface for local data sources that provide aggregated load readings.
/// Implementations connect to depot/fleet systems but only expose aggregated data.
/// </summary>
public interface ILocalDataSource
{
    /// <summary>
    /// Returns aggregated load readings for the specified period.
    /// Must NOT return raw session-level data.
    /// </summary>
    Task<IReadOnlyList<LoadReading>> GetLoadReadingsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the applicable tariff windows for the given period.
    /// </summary>
    Task<IReadOnlyList<TariffWindow>> GetTariffWindowsAsync(
        CancellationToken cancellationToken = default);
}
