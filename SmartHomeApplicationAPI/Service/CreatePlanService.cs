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
        #region Data Retrival
        // 1. Determine optimization horizon.
        // Calculate the 96 x 15-minute intervals.
        // StartTime = request.StartTime
        // EndTime = StartTime + 24 hours
        // Each interval corresponds to one solver step.

        // 2. Retrieve electricity prices.
        // Repository: GetElectricityPricesAsync(...)
        // Retrieves: Time, PricePerKwh, PriceArea, IsPredicted.
        // Service calculates:
        // - Prices: 96 double values, ordered by time.
        // - Validate that all 96 intervals have a price.

        // 3. Retrieve user's devices.
        // Repository: GetUserDevicesAsync(...)
        // Retrieves: UserDevice.Id, UserDevice.Nickname,
        //            DeviceId, Device.Model, Device.Category.
        // Service calculates:
        // - Which devices are controllable.
        // - Which devices are appliances and which are EVs.
        // - Device IDs required for subsequent queries.

        // 4. Retrieve presets for user's devices.
        // Repository: GetDevicePresetsAsync(...)
        // Retrieves: Id, DeviceId, PresetName, ConsumptionInKwh,
        //            DurationInMinutes, BatteryCapacityInKwh.
        // Service calculates:
        // - Appliance PowerProfile.
        // - Appliance duration in 15-minute solver steps.
        // - EV MaxEnergy where battery capacity is available.
        // - Which preset applies to each device.

        // 5. Retrieve existing plans.
        // Repository: GetExistingPlansAsync(...)
        // Retrieves: UserDeviceId, DevicePresetId,
        //            PlanStartTime, PlanEndTime,
        //            WindowStartTime, WindowEndTime,
        //            NextPlanId, ChargeStopPercentage.
        // Service calculates:
        // - ReadyStep.
        // - DeadlineStep.
        // - FixedStart.
        // - Whether a device already has a scheduled plan.
        // - EV EnergyGoal from ChargeStopPercentage, where possible.
        // - Whether continuation plans affect the optimization horizon.

        // 6. Retrieve base load.
        // Repository: GetBaseLoadAsync(...)
        // Retrieves: 96 base-load measurements, if available.
        // Service calculates:
        // - BaseLoad: 96 double values in kW.
        // NOTE: Source/model still needs to be established.

        // 7. Retrieve fuse limit.
        // Repository: GetFuseLimitAsync(...)
        // Retrieves: User installation's maximum electrical capacity.
        // Service calculates:
        // - FuseLimit in kW.
        // NOTE: Source/model still needs to be established.

        // 8. Build controllable loads.
        // Service combines UserDevices, DevicePresets and Plans.
        // For appliances:
        // - Type
        // - Name
        // - PowerProfile
        // - ReadyStep / DeadlineStep OR FixedStart
        //
        // For EVs:
        // - Type
        // - Name
        // - InitialEnergy
        // - EnergyGoal
        // - MaxEnergy
        // - MaxChargingPower
        // NOTE: EV battery state and maximum charging power
        // still need an established data source.

        // 9. Construct SolverReqDTO.
        // - StartTime
        // - Prices
        // - BaseLoad
        // - FuseLimit
        // - Loads
        //
        // Validate that the DTO contains all required solver input.

        #endregion

        // 10. Send SolverReqDTO to Python optimization process.
        // NOT IMPLEMENTED YET.

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