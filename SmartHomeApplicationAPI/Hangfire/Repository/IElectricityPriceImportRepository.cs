using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public interface IElectricityPriceImportRepository
{
    Task UpdateInsertAsync(
        IReadOnlyCollection<DayAheadPrice> prices,
        CancellationToken cancellationToken = default);

    Task<int> CountForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default);
}