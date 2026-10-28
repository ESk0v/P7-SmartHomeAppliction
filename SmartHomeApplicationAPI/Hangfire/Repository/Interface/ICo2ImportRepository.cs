using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public interface ICo2ImportRepository
{
    /// <summary>
    /// One roundtrip. Returns tracked price rows in [from, to), in the same time base as the Time column.
    /// </summary>
    Task<List<Electricity>> GetRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One roundtrip. Writes the CO2 values the caller set on rows from GetRangeAsync. Never inserts.
    /// </summary>
    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}