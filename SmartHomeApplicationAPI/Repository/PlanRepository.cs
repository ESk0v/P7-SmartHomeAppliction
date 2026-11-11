using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public class PlanRepository : IPlanRepository
{
    private readonly SmartHomeDbContext _db;

    public PlanRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<List<PlanDto>> GetPlansAsync(string userId, DateTime date)
    {
        return await _db.Plan
            .Where(p => p.UserDevice.UserId == userId && p.PlanEndTime >= date)
            .AsNoTracking()
            .OrderBy(p => p.PlanStartTime)
            .Select(p => new PlanDto(
                planId: p.Id,
                nextPlanId: p.NextPlanId,
                planName: p.Name,
                icon: p.UserDevice.Icon,
                colorCode: p.UserDevice.Color,
                deviceName: p.UserDevice.Nickname,
                deviceCategory: p.UserDevice.Device!.Category,
                optimizationObjective: p.OptimizationObjective,
                chargeStopPercentage: p.ChargeStopPercentage,
                presetDurationInMinutes: _db.Preset
                    .Where(preset => preset.Id == p.DevicePresetId)
                    .Select(preset => preset.DurationInMinutes)
                    .FirstOrDefault(),
                startTime: p.PlanStartTime,
                endTime: p.PlanEndTime,
                windowStartTime: p.WindowStartTime,
                windowEndTime: p.WindowEndTime,
                estimatedCost: p.EstimatedPrice,
                estimatedCo2Emissions: p.EstimatedCo2Emissions,
                estimatedUnscheduledPrice: p.EstimatedUnscheduledPrice,
                estimatedUnscheduledCo2Emissions: p.EstimatedUnscheduledCo2Emissions,
                batteryCapacityInKwh: _db.Preset
                    .Where(preset => preset.Id == p.DevicePresetId)
                    .Select(preset => preset.BatteryCapacityInKwh)
                    .FirstOrDefault()
            ))
            .ToListAsync();
    }

    public async Task<PlanDTO?> GetPlanForEditAsync(string userId, int planId)
    {
        Plan? plan = await _db.Plan
            .Include(p => p.UserDevice)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserDevice.UserId == userId);

        return plan is null ? null : await ToPlanDtoAsync(plan);
    }

    public Task<PlanUserDeviceDto?> GetUserDeviceForUpdateAsync(string userId, int userDeviceId) =>
        _db.UserDevice
            .AsNoTracking()
            .Where(item => item.Id == userDeviceId && item.UserId == userId)
            .Join(_db.Device,
                item => item.DeviceId,
                device => device.Id,
                (item, device) => new PlanUserDeviceDto(
                    item.Id, item.DeviceId, item.Nickname, item.Icon, item.Color,
                    device.Category, device.Model,
                    _db.Preset
                        .Where(preset => preset.DeviceId == device.Id)
                        .OrderByDescending(preset => preset.PresetName == "Info")
                        .ThenBy(preset => preset.Id)
                        .Select(preset => preset.BatteryCapacityInKwh)
                        .FirstOrDefault()))
            .FirstOrDefaultAsync();

    public Task<bool> PresetBelongsToDeviceAsync(int presetId, int deviceId) =>
        _db.Preset.AnyAsync(preset => preset.Id == presetId && preset.DeviceId == deviceId);

    public async Task<PlanDTO?> UpdatePlanAsync(string userId, int planId, PlanDTO update)
    {
        Plan? plan = await _db.Plan
            .Include(p => p.UserDevice)
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserDevice.UserId == userId);

        if (plan is null)
            return null;

        UserDevice? device = await _db.UserDevice
            .FirstOrDefaultAsync(item => item.Id == update.userDeviceId && item.UserId == userId);

        if (device is null)
            return null;

        plan.Name = update.planName.Trim();
        plan.UserDeviceId = device.Id;
        plan.UserDevice = device;
        plan.WindowStartTime = update.windowStartTime!.Value;
        plan.WindowEndTime = update.windowEndTime!.Value;
        plan.OptimizationObjective = update.optimizationObjective!;
        plan.DevicePresetId = update.presetId;
        plan.ChargeStopPercentage = update.chargeStopPercentage;
        plan.EstimatedUnscheduledPrice = update.estimatedUnscheduledCost!.Value;
        plan.EstimatedUnscheduledCo2Emissions = update.estimatedUnscheduledCo2Emissions!.Value;

        await _db.SaveChangesAsync();
        return await ToPlanDtoAsync(plan);
    }

    private async Task<PlanDTO> ToPlanDtoAsync(Plan plan)
    {
        decimal? batteryCapacity = await _db.Preset
            .Where(preset => preset.DeviceId == plan.UserDevice.DeviceId)
            .OrderByDescending(preset => preset.PresetName == "Info")
            .ThenBy(preset => preset.Id)
            .Select(preset => preset.BatteryCapacityInKwh)
            .FirstOrDefaultAsync();

        return new PlanDTO
        {
            planId = plan.Id,
            userDeviceId = plan.UserDeviceId,
            startTime = plan.PlanStartTime,
            endTime = plan.PlanEndTime,
            windowStartTime = plan.WindowStartTime,
            windowEndTime = plan.WindowEndTime,
            nextPlanId = plan.NextPlanId,
            estimatedCost = plan.EstimatedPrice,
            estimatedCo2Emissions = plan.EstimatedCo2Emissions,
            estimatedUnscheduledCost = plan.EstimatedUnscheduledPrice,
            estimatedUnscheduledCo2Emissions = plan.EstimatedUnscheduledCo2Emissions,
            planName = plan.Name,
            icon = plan.UserDevice.Icon,
            colorCode = plan.UserDevice.Color,
            deviceName = plan.UserDevice.Nickname,
            optimizationObjective = plan.OptimizationObjective,
            presetId = plan.DevicePresetId,
            chargeStopPercentage = plan.ChargeStopPercentage,
            batteryCapacityInKwh = batteryCapacity
        };
    }
}
