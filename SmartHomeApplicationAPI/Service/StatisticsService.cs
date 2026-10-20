using Utilities.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Service;

public class StatisticsService : IStatisticsService
{
    private readonly IStatisticsRepository _repository;

    public StatisticsService(IStatisticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<StatisticsDto> GetPlannedCostAsync(string userID)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);

        var plans = await _repository.GetPlannedCostAsync(userID, start, end);

        if (plans.Count == 0)
        {
            return new StatisticsDto();
        }

        decimal totalPrice = 0;

        foreach (Plan plan in plans)
        {
            totalPrice += plan.EstimatedPrice;
        }

        return new StatisticsDto{
            Value = "" + totalPrice + " Kr."
        };
    }
}