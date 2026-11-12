using Utilities.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.DTOs.Optimization;
using System.Net.Http.Json;
using System.Collections.Concurrent;

namespace SmartHomeApplicationAPI.Service;

public class CreatePlanService : ICreatePlanService
{
    private readonly ICreatePlanRepository _repository;
    private readonly ILogger<CreatePlanService> _logger;
   private const string DefaultOptimisationUrl = "http://127.0.0.1:8001";

    // One client per address, shared across requests (the service itself is created per request)
    private static readonly ConcurrentDictionary<string, HttpClient> Clients = new();
    private readonly HttpClient _http;

    private static readonly System.Text.Json.JsonSerializerOptions _solverJson = new()
    {
        PropertyNamingPolicy = null
    };

    public CreatePlanService(ICreatePlanRepository repository, ILogger<CreatePlanService> logger, IConfiguration? configuration = null)
    {
        _repository = repository;
        _logger = logger;

        // Optimisation:BaseUrl comes from appsettings (8001 locally, set in appsettings.Production.json on the server)
        var baseUrl = configuration?["Optimisation:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = DefaultOptimisationUrl;

        _http = Clients.GetOrAdd(baseUrl, url => new HttpClient
        {
            BaseAddress = new Uri(url),
            Timeout = TimeSpan.FromSeconds(30)
        });
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
        var now = DateTime.UtcNow;
        var start = new DateTime(
            now.Ticks - now.Ticks % TimeSpan.FromMinutes(15).Ticks,
            DateTimeKind.Utc);

        var end = request.EndWindow.ToUniversalTime();

        var prices = await _repository.GetElectricityPricesAsync(request.Area, start, end);

        var devicePresets = await _repository.GetDevicesWithPresetsAsync(request.UserDeviceId);
        var userDeviceIds = devicePresets.Select(x => x.Device.id).Distinct().ToList();

        var plans = await _repository.GetActivePlansAsync(userDeviceIds, start);

        // Step index from step 0. Clamped to 0 so a window that starts before "now" begins at step 0
        // Quarters from step 0 (now rounded down to the quarter). Negative = before step 0
        // Example: now 10:40 -> step 0 = 10:30, 10:45 = 1, 11:00 = 2, 10:00 = -2
        int Step(DateTime t)
        {
            var utc = t.Kind == DateTimeKind.Local
                ? t.ToUniversalTime()
                : DateTime.SpecifyKind(t, DateTimeKind.Utc);   // Unspecified is treated as UTC
            return (int)Math.Floor((utc - start).TotalMinutes / 15.0);
        }

        var loads = new List<SolverLoadDTO>();

        foreach (var group in devicePresets.GroupBy(x => x.Device.id))
        {
            var device = group.First().Device;
            var presets = group.Select(x => x.Preset).ToList();
            var plan = plans.FirstOrDefault(p => p.UserDeviceId == device.id);
            var isRequested = device.id == request.UserDeviceId;

            // Only the requested device and devices that already have a plan are part of the solve
            if (!isRequested && plan is null)
                continue;

            // Requested device uses the preset from the request, planned devices use their plan's preset
            var presetId = isRequested ? request.DevicePresetId : plan!.DevicePresetId;
            var preset = presets.FirstOrDefault(p => p.Id == presetId) ?? presets.First();

            var isEv = device.category == "EV"; // check the real category string in your DB

            var load = new SolverLoadDTO
            {
                Type = isEv ? "ev" : "appliance",
                Name = device.deviceNickname
            };

            if (isEv)
            {
                load.MaxEnergy = (double?)preset.BatteryCapacityInKwh;            // E_max, kWh
                load.MaxChargingPower = (double)preset.ConsumptionInKwh;          // P_ev_max, kW
                load.InitialEnergy = 0.2 * (load.MaxEnergy ?? 0);                 // hardcoded 20%

                // Goal from the existing plan's stop percentage, otherwise hardcoded 100%
                var stopPercentage = (double)(plan?.ChargeStopPercentage ?? 100);
                load.EnergyGoal = stopPercentage / 100.0 * (load.MaxEnergy ?? 0); // E_goal, kWh
            }
            else
            {
                // DurationInMinutes is nullable: no duration = one step
                var steps = Math.Max(1, (int)Math.Ceiling((preset.DurationInMinutes ?? 15) / 15.0));
                load.PowerProfile = Enumerable
                    .Repeat((double)preset.ConsumptionInKwh / steps, steps)       // kWh per 15-min step
                    .ToList();

                if (!isRequested && plan is not null)
                {
                    load.FixedStart = Step(plan.PlanStartTime);                   // already scheduled
                }
                else
                {
                    load.ReadyStep = Step(request.StartWindow);                   // earliest start
                    load.DeadlineStep = Step(request.EndWindow);                  // latest finish
                }
            }

            loads.Add(load);
        }

        _logger.LogInformation(
            "Optimize data: {Prices} prices, {DevicePresets} device/preset rows, {Plans} plans, {Loads} loads",
            prices.Count, devicePresets.Count, plans.Count, loads.Count);

        var solverReq = new SolverReqDTO
        {
            StartTime = start,
            Prices = prices,
            BaseLoad = Enumerable.Repeat(0.5, prices.Count).ToList(),
            FuseLimit = 15.0,
            Loads = loads
        };

        _logger.LogInformation("SolverReq: {Json}",
            System.Text.Json.JsonSerializer.Serialize(solverReq));
        #endregion

        var response = await _http.PostAsJsonAsync("/run", solverReq, _solverJson);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Optimisation failed ({Status}): {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Optimisation failed: {body}");
        }

        _logger.LogInformation("Optimisation result: {Result}", body);

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var root = doc.RootElement;

        // the device we optimise = the appliance that is not fixed, otherwise the EV
        var app = root.GetProperty("appliances").EnumerateArray()
            .Where(a => !a.GetProperty("fixed").GetBoolean())
            .Select(a => (System.Text.Json.JsonElement?)a)
            .FirstOrDefault();
        var item = app ?? root.GetProperty("evs")[0];

        return new OptimizationResultDTO(
            item.GetProperty("start_time").GetDateTime().ToUniversalTime(),
            item.GetProperty("end_time").GetDateTime().ToUniversalTime(),
            Math.Round(item.GetProperty("cost").GetDecimal(), 2),
            20m);   // CO2 hardcoded for now

    }

}