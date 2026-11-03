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
    public async Task<StatisticsDto> GetPlannedCostAsync(string userID)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);

        var plans = await _repository.RetrievePlansAsync(userID, start, end);

        if (plans.Count == 0)
        {
            return new StatisticsDto
            {
                Value = "--",
                Description = "No plans scheduled today"
            };
        }

        decimal totalPrice = 0;

        foreach (Plan plan in plans)
        {
            totalPrice += plan.EstimatedPrice;
        }

        return new StatisticsDto{
            Value = "" + totalPrice + " kr"
        };
    }

    public async Task<StatisticsDto> GetPlannedCo2Async(string userID)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);

        var plans = await _repository.RetrievePlansAsync(userID, start, end);

        if (plans.Count == 0)
        {
            return new StatisticsDto
            {
                Value = "--",
                Description = "No plans scheduled today"
            };
        }

        decimal totalCo2 = 0;

        foreach (Plan plan in plans)
        {
            totalCo2 += plan.EstimatedCo2Emissions;
        }

        
        return new StatisticsDto{
            Value = ""+ totalCo2 + " g"
        };
    }

    public async Task<StatisticsDto> GetScheduleSavingCostAsync(string userID)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);

        var plans = await _repository.RetrievePlansAsync(userID, start, end);

        if (plans.Count == 0)
        {
            return new StatisticsDto
            {
                Value = "--",
                Description = "No plans scheduled today"
            };
        }

        decimal totalPrice = 0;
        decimal totalUnscheduledPrice = 0;

        foreach (Plan plan in plans)
        {
            totalPrice += plan.EstimatedPrice;
            totalUnscheduledPrice += plan.EstimatedUnscheduledPrice;
        }

        decimal savedPrice = totalUnscheduledPrice - totalPrice;
        decimal savedPricePercent = Math.Round((totalUnscheduledPrice - totalPrice) / totalUnscheduledPrice * 100, 2);
        string arrow = savedPrice > 0 ? "\u2193" : "";
        
        return new StatisticsDto{
            Value = "" + savedPrice + " kr",
            Description = "" + arrow + " " + savedPricePercent + " % cheaper"
        };
    }

    public async Task<StatisticsDto> GetScheduleSavingCo2Async(string userID)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);

        var plans = await _repository.RetrievePlansAsync(userID, start, end);

        if (plans.Count == 0)
        {
            return new StatisticsDto
            {
                Value = "--",
                Description = "No plans scheduled today"
            };
        }

        decimal totalCo2 = 0;
        decimal totalUnscheduledCo2 = 0;

        foreach (Plan plan in plans)
        {
            totalCo2 += plan.EstimatedCo2Emissions;
            totalUnscheduledCo2 += plan.EstimatedUnscheduledCo2Emissions;
        }

        decimal savedCo2 = totalUnscheduledCo2 - totalCo2;
        decimal savedCo2Percent = (totalUnscheduledCo2 - totalCo2) / totalUnscheduledCo2 * 100;
        string arrow = savedCo2 > 0 ? "\u2193" : "";
        
        return new StatisticsDto{
            Value = "" + savedCo2 + " g",
            Description = "" + arrow + " " + savedCo2Percent + " % cheaper"
        };
    }

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
