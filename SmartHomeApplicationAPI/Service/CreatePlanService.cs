using FluentValidation;
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

    public async Task<List<OptimizationResultDTO>> OptimizeAsync(OptimizationRequestDTO request)
    {
        #region Data Retrival
        var now = DateTime.UtcNow;
        var quarter = TimeSpan.FromMinutes(15).Ticks;
        var start = new DateTime(
            (now.Ticks + quarter - 1) / quarter * quarter,
            DateTimeKind.Utc);

        var end = request.EndWindow.ToUniversalTime();

        // One day of history before start, so missing CO2 can be filled from the day before
        var slots = await _repository.GetEnergySlotsAsync(request.Area, start.AddDays(-1), end);
        var historyCount = slots.Count(s => s.Time < start);

        var co2WithHistory = slots.Select(s => s.Co2GramsPerKwh).ToList();
        var missingCo2 = co2WithHistory.Skip(historyCount).Count(g => g is null);
        Co2Fill.SeasonalNaive(slots.Select(s => s.Time).ToList(), co2WithHistory);

        var prices = slots.Skip(historyCount).Select(s => s.Price).ToList();     // DKK/kWh
        var horizonCo2 = co2WithHistory.Skip(historyCount).ToList();             // g/kWh
        var unknownCo2 = horizonCo2.Count(g => g is null);

        _logger.LogInformation(
            "CO2: {Filled} of {Total} quarter-hours estimated (seasonal naive)",
            missingCo2 - unknownCo2, prices.Count);

        if (unknownCo2 > 0)
        {
            if (request.Priority == "Greenest")
                throw new ValidationException("No CO2 data is available for this period.");

            _logger.LogWarning(
                "CO2: {Unknown} of {Total} quarter-hours have no CO2 value; they count as 0 g",
                unknownCo2, prices.Count);
        }

        var co2 = horizonCo2.Select(g => g ?? 0).ToList();

        var devicePresets = await _repository.GetDevicesWithPresetsAsync(request.UserDeviceId);
        var userDeviceIds = devicePresets.Select(x => x.Device.id).Distinct().ToList();

        var plans = await _repository.GetActivePlansAsync(userDeviceIds, now);

        int Step(DateTime t)
        {
            var utc = t.Kind == DateTimeKind.Local
                ? t.ToUniversalTime()
                : DateTime.SpecifyKind(t, DateTimeKind.Utc);   // Unspecified is treated as UTC
            return (int)Math.Floor((utc - start).TotalMinutes / 15.0);
        }

        var loads = new List<SolverLoadDTO>();
        SolverLoadDTO? requestedLoad = null;

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

            var isEv = string.Equals(
                device.category?.Trim(),
                "Interruptible",
                StringComparison.OrdinalIgnoreCase
            );

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

                // Goal: the requested percentage for a new plan, the saved one for an existing plan, otherwise 100%
                var stopPercentage = (double)((isRequested ? request.ChargeStopPercentage : plan?.ChargeStopPercentage) ?? 100);
                load.EnergyGoal = stopPercentage / 100.0 * (load.MaxEnergy ?? 0); // E_goal, kWh

                // Window: the solver reads plug-in from ReadyStep and departure from DeadlineStep.
                // Departure is capped at the last known price; beyond it the solver sets no goal and plans no charging.
                var windowStart = isRequested ? request.StartWindow : plan!.WindowStartTime;
                var windowEnd = isRequested ? request.EndWindow : plan!.WindowEndTime;
                load.ReadyStep = Math.Max(0, Step(windowStart));                  // t_plug
                load.DeadlineStep = Math.Min(Step(windowEnd), prices.Count);       // t_dep
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

            if (isRequested)
                requestedLoad = load;

            loads.Add(load);
        }

        _logger.LogInformation(
            "Optimize data: {Prices} prices, {DevicePresets} device/preset rows, {Plans} plans, {Loads} loads",
            prices.Count, devicePresets.Count, plans.Count, loads.Count);

        var solverReq = new SolverReqDTO
        {
            StartTime = start,
            Prices = request.Priority == "Greenest"
                ? co2.Select(g => g / 1000.0).ToList()                              // kg/kWh, keeps the EV penalty dominant
                : prices,
            BaseLoad = Enumerable.Repeat(0.5, prices.Count).ToList(),
            FuseLimit = 15.0,
            Loads = loads
        };

        _logger.LogInformation("SolverReq: {Json}",
            System.Text.Json.JsonSerializer.Serialize(solverReq));
        #endregion

        // A program whose own power is above what the fuse allows can never be planned
        if (requestedLoad?.PowerProfile is { Count: > 0 } profile)
        {
            var programKw = profile.Max() / 0.25;                                     // kWh per step -> kW
            var availableKw = solverReq.FuseLimit - solverReq.BaseLoad.DefaultIfEmpty(0).Max();
            if (programKw > availableKw)
                throw new ValidationException(
                    $"The selected program uses {programKw:0.#} kW, but only {availableKw:0.#} kW is available " +
                    $"under the fuse limit of {solverReq.FuseLimit} kW. Choose another preset.");
        }

        var response = await _http.PostAsJsonAsync("/run", solverReq, _solverJson);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Optimisation failed ({Status}): {Body}", (int)response.StatusCode, body);

            // Infeasible: the load does not fit under the fuse limit together with the base load and other plans
            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity && body.Contains("Infeasible"))
                throw new ValidationException(
                    $"The plan cannot be created, as it would exceed the fuse limit of {solverReq.FuseLimit} kW " +
                    "together with your other plans. Choose a longer time window or another time.");

            throw new InvalidOperationException($"Optimisation failed: {body}");
        }

        _logger.LogInformation("Optimisation result: {Result}", body);

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var root = doc.RootElement;

        var results = new List<OptimizationResultDTO>();

        // APPLIANCE
        var app = root.GetProperty("appliances").EnumerateArray()
            .Where(a => !a.GetProperty("fixed").GetBoolean())
            .Select(a => (System.Text.Json.JsonElement?)a)
            .FirstOrDefault();

        if (app.HasValue)
        {
            var item = app.Value;

            // Price and CO2 from the scheduled profile (kWh per step); the solver's cost is in the objective's unit
            var startStep = item.GetProperty("start_step").GetInt32();
            var appProfile = requestedLoad?.PowerProfile ?? [];
            decimal appCost = 0m;
            decimal appCo2 = 0m;

            for (int i = 0; i < appProfile.Count && startStep + i < prices.Count; i++)
            {
                appCost += (decimal)appProfile[i] * (decimal)prices[startStep + i];
                appCo2 += (decimal)appProfile[i] * (decimal)co2[startStep + i];
            }

            results.Add(new OptimizationResultDTO(
                item.GetProperty("start_time").GetDateTime().ToUniversalTime(),
                item.GetProperty("end_time").GetDateTime().ToUniversalTime(),
                Math.Round(appCost, 2),
                Math.Round(appCo2)
            ));

            return results;
        }

        // EV
        var evs = root.GetProperty("evs");

        if (evs.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Solver returned no optimizable appliance or EV.");
        }

        var ev = evs[0];

        var powers = ev.GetProperty("power_kw")
            .EnumerateArray()
            .Select(x => x.GetDouble())
            .ToArray();

        int? chargingStart = null;

        for (int t = 0; t <= powers.Length; t++)
        {
            bool charging = t < powers.Length && powers[t] > 0.0001;

            if (charging && chargingStart is null)
            {
                chargingStart = t;
            }
            else if (!charging && chargingStart is not null)
            {
                int from = chargingStart.Value;
                int to = t;

                decimal intervalCost = 0m;
                decimal intervalCo2 = 0m;

                for (int i = from; i < to; i++)
                {
                    intervalCost +=
                        (decimal)powers[i] *
                        0.25m *
                        (decimal)prices[i];
                    intervalCo2 +=
                        (decimal)powers[i] *
                        0.25m *
                        (decimal)co2[i];
                }

                results.Add(new OptimizationResultDTO(
                    start.AddMinutes(from * 15),
                    start.AddMinutes(to * 15),
                    Math.Round(intervalCost, 2),
                    Math.Round(intervalCo2)
                ));

                chargingStart = null;
            }
        }

        if (results.Count == 0)
        {
            throw new InvalidOperationException(
                "Solver returned no charging intervals for EV.");
        }

        return results;
    }

}