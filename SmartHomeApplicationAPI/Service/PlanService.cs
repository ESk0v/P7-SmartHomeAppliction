using FluentValidation;
using Utilities.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Service;

public class PlanService : IPlanService
{
    private readonly IPlanRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly IElectricityPriceService _priceService;

    public PlanService(IPlanRepository repository, IElectricityPriceService priceService, TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _priceService = priceService;
    }

    public async Task<List<PlanDto>> GetPlansAsync(string userID)
    {
        DateTime date = DateTime.UtcNow;

        return await _repository.GetPlansAsync(userID, date);
    }

    public Task<PlanDTO?> GetPlanForEditAsync(string userId, int planId) =>
        _repository.GetPlanForEditAsync(userId, planId);

    public async Task<bool> DeletePlanAsync(string userId, int planId)
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        var ownedPlans = (await _repository.GetUserPlansAsync(userId)).ToDictionary(plan => plan.Id);
        if (!ownedPlans.ContainsKey(planId)) return false;

        var toDelete = GetUnfinishedChain(ownedPlans, planId, now);
        RepairLinks(ownedPlans, toDelete);
        await _repository.DeletePlansAsync(toDelete);
        return true;
    }

    public async Task<PlanDTO?> SaveGeneratedPlanAsync(string userId, SaveGeneratedPlanRequestDto request)
    {
        var ownedPlans = new Dictionary<int, Plan>();
        var removed = new List<Plan>();
        if (request.ReplacesPlanId is int originalId)
        {
            if (originalId <= 0) throw new ValidationException("Select a valid plan to replace.");
            ownedPlans = (await _repository.GetUserPlansAsync(userId)).ToDictionary(plan => plan.Id);
            if (!ownedPlans.ContainsKey(originalId)) return null;
            removed = GetUnfinishedChain(ownedPlans, originalId, _timeProvider.GetUtcNow().UtcDateTime);
            if (removed.Count == 0)
                throw new ValidationException("This plan has finished and can no longer be replaced.");
        }

        if (request.Intervals is null || request.Intervals.Count == 0 || request.Intervals.Any(interval => interval is null))
            throw new ValidationException("Generate a plan before saving.");
        if (request.AreaCode is not ("DK1" or "DK2"))
            throw new ValidationException("Select DK1 or DK2.");

        var device = await _repository.GetUserDeviceForUpdateAsync(userId, request.UserDeviceId);
        var preset = request.PresetId is int presetId ? await _repository.GetPresetForSaveAsync(presetId) : null;
        var settings = new PlanDTO
        {
            userDeviceId = request.UserDeviceId, planName = request.PlanName,
            windowStartTime = request.WindowStart, windowEndTime = request.WindowEnd,
            optimizationObjective = request.OptimizationObjective, presetId = request.PresetId,
            chargeStopPercentage = request.ChargeStopPercentage
        };
        var validator = CreateUpdateValidator(device, preset is not null && preset.DeviceId == device?.deviceId);
        var validation = await validator.ValidateAsync(settings);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var ordered = request.Intervals.OrderBy(interval => interval.StartTime).ToList();
        for (int index = 0; index < ordered.Count; index++)
        {
            var interval = ordered[index];
            if (interval.StartTime.Kind != DateTimeKind.Utc || interval.EndTime.Kind != DateTimeKind.Utc ||
                interval.EndTime <= interval.StartTime || interval.StartTime < request.WindowStart ||
                interval.EndTime > request.WindowEnd ||
                (index > 0 && interval.StartTime < ordered[index - 1].EndTime))
                throw new ValidationException("Plan blocks must be valid UTC intervals inside the scheduling window, without overlaps.");
        }

        // Match the optimizer's current 20% starting charge when calculating the baseline workload.
        decimal energy = device!.category == "Interruptible"
            ? (device.batteryCapacityInKwh ?? 0m) * Math.Max(request.ChargeStopPercentage!.Value - 20, 0) / 100m
            : preset!.ConsumptionInKwh;
        if (energy <= 0) throw new ValidationException("A positive workload is required to calculate the unscheduled baseline.");
        long totalTicks = ordered.Sum(interval => (interval.EndTime - interval.StartTime).Ticks);
        var baseline = await _priceService.GetUnscheduledEstimateAsync(new UnscheduledEstimateRequestDto
        {
            AreaCode = request.AreaCode, StartTime = request.WindowStart,
            EndTime = request.WindowStart.AddTicks(totalTicks), EnergyInKwh = energy
        });
        if (baseline.Estimate is null)
            throw new ValidationException(baseline.Error ?? "Could not calculate the unscheduled baseline.");

        decimal remainingPrice = baseline.Estimate.EstimatedPrice;
        decimal remainingCo2 = baseline.Estimate.EstimatedCo2Emissions;
        var plans = new List<Plan>();
        for (int index = 0; index < ordered.Count; index++)
        {
            var interval = ordered[index];
            decimal durationTicks = (interval.EndTime - interval.StartTime).Ticks;
            bool last = index == ordered.Count - 1;
            decimal price = last ? remainingPrice : baseline.Estimate.EstimatedPrice * durationTicks / totalTicks;
            decimal co2 = last ? remainingCo2 : baseline.Estimate.EstimatedCo2Emissions * durationTicks / totalTicks;
            plans.Add(new Plan
            {
                Name = request.PlanName.Trim(), UserDeviceId = request.UserDeviceId,
                PlanStartTime = interval.StartTime, PlanEndTime = interval.EndTime,
                WindowStartTime = request.WindowStart, WindowEndTime = request.WindowEnd,
                OptimizationObjective = request.OptimizationObjective, DevicePresetId = request.PresetId,
                ChargeStopPercentage = request.ChargeStopPercentage,
                EstimatedPrice = interval.EstimatedPrice, EstimatedCo2Emissions = interval.EstimatedCo2Emissions,
                EstimatedUnscheduledPrice = price, EstimatedUnscheduledCo2Emissions = co2
            });
            remainingPrice -= price;
            remainingCo2 -= co2;
        }
        for (int index = 0; index + 1 < plans.Count; index++) plans[index].NextPlan = plans[index + 1];

        RepairLinks(ownedPlans, removed);
        await _repository.SavePlansAsync(removed, plans);
        settings.planId = plans[0].Id;
        settings.nextPlanId = plans[0].NextPlan?.Id;
        settings.planName = plans[0].Name;
        settings.startTime = plans[0].PlanStartTime;
        settings.endTime = plans[0].PlanEndTime;
        settings.estimatedCost = plans[0].EstimatedPrice;
        settings.estimatedCo2Emissions = plans[0].EstimatedCo2Emissions;
        settings.estimatedUnscheduledCost = plans[0].EstimatedUnscheduledPrice;
        settings.estimatedUnscheduledCo2Emissions = plans[0].EstimatedUnscheduledCo2Emissions;
        settings.batteryCapacityInKwh = device.batteryCapacityInKwh;
        return settings;
    }

    private static List<Plan> GetUnfinishedChain(Dictionary<int, Plan> ownedPlans, int planId, DateTime now)
    {
        Plan? current = ownedPlans[planId];

        var chain = new List<Plan>();
        var visited = new HashSet<int>();
        while (current is not null && visited.Add(current.Id))
        {
            chain.Add(current);
            current = current.NextPlanId is int nextId && ownedPlans.TryGetValue(nextId, out var next) ? next : null;
        }

        // A block is unfinished until its end time, including blocks currently running.
        return chain.Where(plan => plan.PlanEndTime > now).ToList();
    }

    private static void RepairLinks(Dictionary<int, Plan> ownedPlans, List<Plan> toDelete)
    {
        var deletedIds = toDelete.Select(plan => plan.Id).ToHashSet();

        // Preserve completed blocks and reconnect their links past the removed intervals.
        foreach (var plan in ownedPlans.Values.Where(plan => !deletedIds.Contains(plan.Id)))
        {
            if (plan.NextPlanId is not int linkedId || !deletedIds.Contains(linkedId)) continue;
            int? nextId = linkedId;
            var skipped = new HashSet<int>();
            while (nextId is int id && deletedIds.Contains(id) && skipped.Add(id))
                nextId = ownedPlans[id].NextPlanId;

            if (nextId is int remaining && deletedIds.Contains(remaining)) nextId = null;
            plan.NextPlan = nextId is int keptId ? ownedPlans.GetValueOrDefault(keptId) : null;
            plan.NextPlanId = nextId;
        }
    }

    public async Task<PlanDTO?> UpdatePlanAsync(string userId, int planId, PlanDTO update)
    {
        PlanUserDeviceDto? device = await _repository.GetUserDeviceForUpdateAsync(userId, update.userDeviceId);
        bool presetBelongsToDevice = device is not null && update.presetId is int presetId &&
            await _repository.PresetBelongsToDeviceAsync(presetId, device.deviceId);

        var validator = CreateUpdateValidator(device, presetBelongsToDevice);
        var result = await validator.ValidateAsync(update);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        return await _repository.UpdatePlanAsync(userId, planId, update);
    }

    private static InlineValidator<PlanDTO> CreateUpdateValidator(
        PlanUserDeviceDto? device, bool presetBelongsToDevice)
    {
        var validator = new InlineValidator<PlanDTO>();

        validator.RuleFor(plan => plan.userDeviceId)
            .Must(_ => device is not null)
            .WithMessage("Select one of your devices.");

        validator.RuleFor(plan => plan.planName)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Enter a plan name.")
            .MaximumLength(100)
            .WithMessage("The plan name cannot exceed 100 characters.");

        validator.RuleFor(plan => plan.optimizationObjective)
            .Must(objective => objective is "Cheapest" or "Greenest")
            .WithMessage("Select an optimization priority.");

        validator.RuleFor(plan => plan.windowStartTime)
            .NotNull().WithMessage("Select a scheduling window.");

        validator.RuleFor(plan => plan.windowStartTime)
            .Must(time => time!.Value.Kind == DateTimeKind.Utc)
            .When(plan => plan.windowStartTime.HasValue)
            .WithMessage("The scheduling window start must be UTC.");

        validator.RuleFor(plan => plan.windowEndTime)
            .NotNull().WithMessage("Select a scheduling window.");

        validator.RuleFor(plan => plan.windowEndTime)
            .Must(time => time!.Value.Kind == DateTimeKind.Utc)
            .When(plan => plan.windowEndTime.HasValue)
            .WithMessage("The scheduling window end must be UTC.");

        validator.RuleFor(plan => plan.windowEndTime)
            .GreaterThan(plan => plan.windowStartTime)
            .When(plan => plan.windowStartTime.HasValue && plan.windowEndTime.HasValue)
            .WithMessage("The scheduling window must have a start before its end.");

        validator.RuleFor(plan => plan.chargeStopPercentage)
            .InclusiveBetween(0, 100)
            .When(plan => plan.chargeStopPercentage.HasValue)
            .WithMessage("The charge percentage must be between 0 and 100.");

        validator.When(_ => device?.category == "Interruptible", () =>
        {
            validator.RuleFor(plan => plan.chargeStopPercentage)
                .NotNull().WithMessage("Select a charge percentage.");
        });

        validator.When(_ => device is not null && device.category != "Interruptible", () =>
        {
            validator.RuleFor(plan => plan.presetId)
                .NotNull().WithMessage("Select a device preset.");
        });

        validator.RuleFor(plan => plan.presetId)
            .Must(_ => presetBelongsToDevice)
            .When(plan => device is not null && plan.presetId.HasValue)
            .WithMessage("The selected preset does not belong to the selected device.");

        return validator;
    }
}
