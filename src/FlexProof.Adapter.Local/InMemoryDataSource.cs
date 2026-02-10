namespace FlexProof.Adapter.Local;

/// <summary>
/// In-memory data source for testing and pilot scenarios.
/// Loaded with synthetic or imported aggregated readings.
/// </summary>
public sealed class InMemoryDataSource : ILocalDataSource
{
    private readonly List<LoadReading> _readings = [];
    private readonly List<TariffWindow> _tariffWindows = [];

    public void AddReadings(IEnumerable<LoadReading> readings)
        => _readings.AddRange(readings);

    public void AddTariffWindows(IEnumerable<TariffWindow> windows)
        => _tariffWindows.AddRange(windows);

    public Task<IReadOnlyList<LoadReading>> GetLoadReadingsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var filtered = _readings
            .Where(r => r.IntervalStart >= from && r.IntervalStart < to)
            .OrderBy(r => r.IntervalStart)
            .ToList();

        return Task.FromResult<IReadOnlyList<LoadReading>>(filtered);
    }

    public Task<IReadOnlyList<TariffWindow>> GetTariffWindowsAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TariffWindow>>(_tariffWindows.AsReadOnly());
    }
}
