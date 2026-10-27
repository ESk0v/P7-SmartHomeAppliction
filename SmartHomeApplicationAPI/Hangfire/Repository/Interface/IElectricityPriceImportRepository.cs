using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public interface IElectricityPriceImportRepository
{
    /// <summary>One roundtrip. Returns the tracked rows in [from, to).</summary>
    Task<List<Electricity>> GetRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>One roundtrip. Inserts the new rows and saves changes made to rows from GetRangeAsync.</summary>
    Task SaveAsync(
        IReadOnlyCollection<Electricity> newRows,
        CancellationToken cancellationToken = default);
}