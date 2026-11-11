using FluentValidation;
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

    public async Task<List<PlanDto>> GetPlansAsync(string userID)
    {
        DateTime date = DateTime.UtcNow;

        return await _repository.GetPlansAsync(userID, date);
    }

    public Task<PlanDTO?> GetPlanForEditAsync(string userId, int planId) =>
        _repository.GetPlanForEditAsync(userId, planId);

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
