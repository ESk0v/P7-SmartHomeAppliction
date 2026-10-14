using Utilities.Models;
using SmartHomeApplicationAPI.Repository;

namespace SmartHomeApplicationAPI.Service;

public class PlanService : IPlanService
{
    private readonly IPlanRepository _repository;

    public PlanService(IPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PlanDto>> GetPlansAsync()
    {
        DateTime date = DateTime.UtcNow;

        return await _repository.GetPlansAsync(date);
    }
}