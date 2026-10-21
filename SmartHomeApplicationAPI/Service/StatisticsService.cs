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

        var plans = await _repository.RetrievePlansAsync(userID, start, end);

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

    public async Task<StatisticsDto> GetScheduleSavingAsync(string userID)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);

        var plans = await _repository.RetrievePlansAsync(userID, start, end);

        if (plans.Count == 0)
        {
            return new StatisticsDto();
        }

        decimal totalPrice = 0;
        decimal totalUnscheduledPrice = 0;

        foreach (Plan plan in plans)
        {
            totalPrice += plan.EstimatedPrice;
            totalUnscheduledPrice += plan.EstimatedUnscheduledPrice;
        }

        decimal savedPrice = totalUnscheduledPrice - totalPrice;
        decimal savedPricePercent = (totalUnscheduledPrice - totalPrice) / totalUnscheduledPrice * 100;
        string arrow = savedPrice >= 0 ? "\u2193" : "\u2191";
        
        return new StatisticsDto{
            Value = "" + savedPrice + " Kr.",
            Description = 
                "" +
                 arrow + 
                savedPricePercent + 
                " % cheaper"
        };
    }
}