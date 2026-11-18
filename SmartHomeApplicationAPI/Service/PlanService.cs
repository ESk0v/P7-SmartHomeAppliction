using FluentValidation;
using Utilities.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Service;

public class PlanService : IPlanService
{
    private readonly IPlanRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PlanService(IPlanRepository repository, TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _timeProvider = timeProvider ?? TimeProvider.System;
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
        if (!ownedPlans.TryGetValue(planId, out var current)) return false;

        var chain = new List<Plan>();
        var visited = new HashSet<int>();
        while (current is not null && visited.Add(current.Id))
        {
            chain.Add(current);
            current = current.NextPlanId is int nextId && ownedPlans.TryGetValue(nextId, out var next) ? next : null;
        }

        // A block is unfinished until its end time, including blocks currently running.
        var toDelete = chain.Where(plan => plan.PlanEndTime > now).ToList();
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

        await _repository.DeletePlansAsync(toDelete);
        return true;
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
