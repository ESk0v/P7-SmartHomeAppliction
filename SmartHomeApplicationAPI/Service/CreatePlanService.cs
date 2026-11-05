using Utilities.Models;
using SmartHomeApplicationAPI.Repository;

namespace SmartHomeApplicationAPI.Service;

public class CreatePlanService : ICreatePlanService
{
    private readonly ICreatePlanRepository _repository;
    private readonly ILogger<CreatePlanService> _logger;
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("http://127.0.0.1:8001"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    public CreatePlanService(ICreatePlanRepository repository, ILogger<CreatePlanService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<PresetDto>> GetPresetsAsync(int deviceId)
    {
        return await _repository.GetPresetsAsync(deviceId);
    }

    public async Task<List<PlanUserDeviceDto>> GetUserDevicesAsync(string userId)
    {
        return await _repository.GetUserDevicesAsync(userId);
    }

    public async Task<PlanDTO> PostPlanAsync(PlanDTO planDTO)
    {
        return await _repository.PostPlanAsync(planDTO);
    }

    public async Task<OptimizationResultDTO> OptimizeAsync(OptimizationRequestDTO request)
    {
        var optimisationResult = await _http.GetStringAsync("/hello");
        _logger.LogInformation("Optimisation responded: {Result}", optimisationResult);

        var result = new OptimizationResultDTO(
            request.StartTime,
            request.EndTime,
            8.5m, 
            20
        );

        return result;

    }

}