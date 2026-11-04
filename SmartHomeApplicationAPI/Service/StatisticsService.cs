using Utilities.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Infrastructure.Models;
using System.Globalization;

namespace SmartHomeApplicationAPI.Service;

public class StatisticsService : IStatisticsService
{
    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");
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
    private static StatisticsDto BuildSavingDto(
        decimal planned, decimal unscheduled, string unit, string betterText, string worseText)
    {
        decimal diff = unscheduled - planned;   // > 0 = better than unscheduled

        if (diff == 0)
        {
            return new StatisticsDto
            {
                Value = $"0 {unit}",
                Description = "Same as unscheduled"
            };
        }

        bool better = diff > 0;
        string arrow = better ? "\u2193" : "\u2191";
        string text = better ? betterText : worseText;
        string value = $"{diff:0.##} {unit}";

        // Baseline is 0 (and plan is worse): percent is undefined, show absolute only
        if (unscheduled == 0)
        {
            return new StatisticsDto
            {
                Value = value,
                Description = $"{arrow} {text}"
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

    #region Optimization Plan Statistics

    public async Task<StatisticsDto> GetEstimatedCostAsync(string userID, int planId)
    {
        var blocks = await RetrievePlanBlocksAsync(userID, planId);
        if (blocks.Count == 0)
            return NoPlanData();

        decimal cost = blocks.Sum(block => block.EstimatedPrice);
        decimal unscheduledCost = blocks.Sum(block => block.EstimatedUnscheduledPrice);
        string description;

        if (unscheduledCost <= 0)
        {
            description = "No unscheduled estimate available";
        }
        else
        {
            decimal differencePercent = (unscheduledCost - cost) / unscheduledCost * 100;
            string percent = Math.Abs(differencePercent).ToString("0.#", DanishCulture);
            description = differencePercent switch
            {
                > 0 => $"↓ {percent}% cheaper vs. unscheduled",
                < 0 => $"{percent}% more than unscheduled",
                _ => "Same as unscheduled"
            };
        }

        return new StatisticsDto
        {
            Value = FormatKroner(cost),
            Description = description
        };
    }

    public async Task<StatisticsDto> GetMoneySavedAsync(string userID, int planId)
    {
        var blocks = await RetrievePlanBlocksAsync(userID, planId);
        if (blocks.Count == 0)
            return NoPlanData();

        decimal cost = blocks.Sum(block => block.EstimatedPrice);
        decimal unscheduledCost = blocks.Sum(block => block.EstimatedUnscheduledPrice);

        return new StatisticsDto
        {
            Value = FormatKroner(unscheduledCost - cost),
            Description = unscheduledCost <= 0
                ? "No unscheduled estimate available"
                : "vs. unscheduled"
        };
    }

    public async Task<StatisticsDto> GetLoadDurationAsync(string userID, int planId)
    {
        var blocks = await RetrievePlanBlocksAsync(userID, planId);
        if (blocks.Count == 0)
            return NoPlanData();

        TimeSpan duration = blocks.Aggregate(TimeSpan.Zero,
            (total, block) => total + (block.PlanEndTime - block.PlanStartTime));
        DateTime start = blocks.Min(block => block.PlanStartTime);

        return new StatisticsDto
        {
            Value = FormatDuration(duration),
            Description = $"Starts at {start.ToString("HH:mm", CultureInfo.InvariantCulture)}"
        };
    }

    public async Task<StatisticsDto> GetLoadFinishAsync(string userID, int planId)
    {
        var blocks = await RetrievePlanBlocksAsync(userID, planId);
        if (blocks.Count == 0)
            return NoPlanData();

        DateTime finish = blocks.Max(block => block.PlanEndTime);
        TimeSpan timeBeforeDeadline = blocks[0].WindowEndTime - finish;
        string description = timeBeforeDeadline > TimeSpan.Zero
            ? $"{FormatDuration(timeBeforeDeadline)} before deadline"
            : "Finishes right on deadline";

        return new StatisticsDto
        {
            Value = finish.ToString("HH:mm", CultureInfo.InvariantCulture),
            Description = description
        };
    }

    private async Task<List<Plan>> RetrievePlanBlocksAsync(string userID, int planId)
    {
        var blocks = new List<Plan>();
        var visited = new HashSet<int>();
        int? currentId = planId;

        while (currentId is int id && visited.Add(id))
        {
            Plan? block = await _repository.RetrievePlanByIdAsync(userID, id);
            if (block is null)
                break;

            blocks.Add(block);
            currentId = block.NextPlanId;
        }

        return blocks;
    }

    private static StatisticsDto NoPlanData() => new()
    {
        Value = "--",
        Description = "No plan data available"
    };

    private static string FormatKroner(decimal amount) =>
        $"{amount.ToString("0.00", DanishCulture)} kr";

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
            : $"{duration.Minutes}m";
  #endregion

}
