using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Hangfire.Repository;

public interface IElectricityPriceImportRepository
{
    Task UpdateInsertAsync(
        IReadOnlyCollection<Electricity> prices,
        CancellationToken cancellationToken = default);

    Task<int> CountForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default);
}