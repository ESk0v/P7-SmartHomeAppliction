namespace SmartHomeApplicationAPI.Repository;

public record PlanSummary(DateTime StartTime, decimal Price, decimal Co2);

public interface IHistoryRepository
{
    Task<List<PlanSummary>> RetrievePlanSummariesAsync(string userId, DateTime start, DateTime end);
}