using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public class CreatePlanRepository : ICreatePlanRepository
{
    private readonly SmartHomeDbContext _db;

    public CreatePlanRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<List<PresetDto>> GetPresetsAsync(int deviceId)
    {
        return await _db.Preset
            .Where(p => p.DeviceId == deviceId)
            .OrderByDescending(p => p.PresetName == "Info")
            .ThenBy(p => p.Id)
            .Select(p => new PresetDto{
                id = p.Id,
                presetName = p.PresetName,
                consumption = p.ConsumptionInKwh,
                duration = p.DurationInMinutes,
                batteryCapacityInKwh = p.BatteryCapacityInKwh
            })
            .ToListAsync();
        
    }

    public async Task<List<PlanUserDeviceDto>> GetUserDevicesAsync(string userId)
    {
        return await _db.UserDevice
            .AsNoTracking()
            .Where(ud => ud.UserId == userId)
            .Join(_db.Device,
                ud => ud.DeviceId,
                d => d.Id,
                (ud, d) => new PlanUserDeviceDto(
                    ud.Id,
                    ud.DeviceId,
                    ud.Nickname,
                    ud.Icon,
                    ud.Color,
                    d.Category,
                    d.Model,
                    _db.Preset
                        .Where(preset => preset.DeviceId == d.Id)
                        .OrderByDescending(preset => preset.PresetName == "Info")
                        .ThenBy(preset => preset.Id)
                        .Select(preset => preset.BatteryCapacityInKwh)
                        .FirstOrDefault()
                ))
            .ToListAsync();
    }

    public async Task<PlanDTO> PostPlanAsync(PlanDTO planDTO)
    {

        var plan = new Plan
        {
            Name = planDTO.planName,
            UserDeviceId = planDTO.userDeviceId,
            PlanStartTime = planDTO.startTime.ToUniversalTime(),
            PlanEndTime = planDTO.endTime.ToUniversalTime(),
            WindowStartTime = (planDTO.windowStartTime ?? planDTO.startTime).ToUniversalTime(),
            WindowEndTime = (planDTO.windowEndTime ?? planDTO.endTime).ToUniversalTime(),
            OptimizationObjective = planDTO.optimizationObjective!,
            DevicePresetId = planDTO.presetId,
            ChargeStopPercentage = planDTO.chargeStopPercentage,
            EstimatedPrice = planDTO.estimatedCost,
            EstimatedCo2Emissions = planDTO.estimatedCo2Emissions,
            EstimatedUnscheduledPrice = (decimal)planDTO.estimatedUnscheduledCost!,
            EstimatedUnscheduledCo2Emissions = (decimal)planDTO.estimatedUnscheduledCo2Emissions!
            
        };

        _db.Plan.Add(plan);

        await _db.SaveChangesAsync();

        decimal? batteryCapacity = await _db.UserDevice
            .Where(device => device.Id == plan.UserDeviceId)
            .Select(device => _db.Preset
                .Where(preset => preset.DeviceId == device.DeviceId)
                .OrderByDescending(preset => preset.PresetName == "Info")
                .ThenBy(preset => preset.Id)
                .Select(preset => preset.BatteryCapacityInKwh)
                .FirstOrDefault())
            .FirstOrDefaultAsync();

        return new PlanDTO
        {
            planId = plan.Id,
            planName = plan.Name,
            startTime = plan.PlanStartTime,
            endTime = plan.PlanEndTime,
            windowStartTime = plan.WindowStartTime,
            windowEndTime = plan.WindowEndTime,
            estimatedCost = plan.EstimatedPrice,
            estimatedCo2Emissions = plan.EstimatedCo2Emissions,
            estimatedUnscheduledCost = plan.EstimatedUnscheduledPrice,
            estimatedUnscheduledCo2Emissions = plan.EstimatedUnscheduledCo2Emissions,
            userDeviceId = plan.UserDeviceId,
            optimizationObjective = plan.OptimizationObjective,
            presetId = plan.DevicePresetId,
            chargeStopPercentage = plan.ChargeStopPercentage,
            batteryCapacityInKwh = batteryCapacity
        };
          
    
    }

#region Optimization

#endregion
}
