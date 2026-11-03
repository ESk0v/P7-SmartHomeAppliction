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

    #region Dashboard Statistics
    public async Task<StatisticsDto> GetPlannedCostAsync(string userID, DateTime from, DateTime to)
    {
        var plans = await _repository.RetrievePlansAsync(userID, ToUtc(from), ToUtc(to));

        if (plans.Count == 0)
            return new StatisticsDto { Value = "--", Description = "No plans in this period" };

        return new StatisticsDto { Value = $"{plans.Sum(p => p.EstimatedPrice)} kr" };
    }

    public async Task<StatisticsDto> GetPlannedCo2Async(string userID, DateTime from, DateTime to)
    {
        var plans = await _repository.RetrievePlansAsync(userID, ToUtc(from), ToUtc(to));

        if (plans.Count == 0)
            return new StatisticsDto { Value = "--", Description = "No plans in this period" };

        return new StatisticsDto { Value = $"{plans.Sum(p => p.EstimatedCo2Emissions)} g" };
    }

    public async Task<StatisticsDto> GetScheduleSavingCostAsync(string userID, DateTime from, DateTime to)
    {
        var plans = await _repository.RetrievePlansAsync(userID, ToUtc(from), ToUtc(to));

        if (plans.Count == 0)
            return new StatisticsDto { Value = "--", Description = "No plans in this period" };

        decimal planned = plans.Sum(p => p.EstimatedPrice);
        decimal unscheduled = plans.Sum(p => p.EstimatedUnscheduledPrice);

        return BuildSavingDto(planned, unscheduled, "kr", "cheaper", "more expensive");
    }

    public async Task<StatisticsDto> GetScheduleSavingCo2Async(string userID, DateTime from, DateTime to)
    {
        var plans = await _repository.RetrievePlansAsync(userID, ToUtc(from), ToUtc(to));

        if (plans.Count == 0)
            return new StatisticsDto { Value = "--", Description = "No plans in this period" };

        decimal planned = plans.Sum(p => p.EstimatedCo2Emissions);
        decimal unscheduled = plans.Sum(p => p.EstimatedUnscheduledCo2Emissions);

        return BuildSavingDto(planned, unscheduled, "g", "less CO2", "more CO2");
    }

    #region Helpers
    private static StatisticsDto BuildSavingDto( decimal planned, decimal unscheduled, string unit, string betterText, string worseText)
    {
        decimal diff = unscheduled - planned;

        if (diff == 0)
        {
            return new StatisticsDto
            {
                Value = $"0 {unit}",
                Description = "0 % saved"
            };
        }

        bool better = diff > 0;
        string arrow = better ? "\u2193" : "\u2191";
        string text = better ? betterText : worseText;
        string value = $"{diff:0.##} {unit}";

        if (unscheduled == 0)
        {
            return new StatisticsDto
            {
                Value = value,
                Description = $"{arrow} {text} than unscheduled"
            };
        }

        decimal percent = Math.Round(Math.Abs(diff) / unscheduled * 100, 1);

        return new StatisticsDto
        {
            Value = value,
            Description = $"{arrow} {percent:0.#} % {text}"
        };
    }
    
    private static readonly TimeZoneInfo CopenhagenTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    private static DateTime ToUtc(DateTime localDate) =>
        TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified), CopenhagenTz);

    #endregion

    #endregion

}