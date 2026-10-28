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
            .Select(p => new PresetDto{
                id = p.Id,
                presetName = p.PresetName,
                consumption = p.ConsumptionInKwh,
                duration = p.DurationInMinutes
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
                    d.Category,
                    d.Model
                ))
            .ToListAsync();
    }

    public async Task<PlanDTO> PostPlanAsync(PlanDTO planDTO)
    {
        if (planDTO.NextPlan != null)
        {
            
        }

        var plan = new Plan
        {
            Name = planDTO.Name,
            UserDeviceId = planDTO.UserDeviceId,
            PlanStartTime = planDTO.StartTime,
            PlanEndTime = planDTO.EndTime,
            WindowStartTime = planDTO.StartTime,
            WindowEndTime = planDTO.EndTime,
            OptimizationObjective = "Cheapest",
            EstimatedPrice = planDTO.EstimatedPrice,
            EstimatedCo2Emissions = planDTO.EstimatedCo2Emission,
            EstimatedUnscheduledPrice = planDTO.EstimatedUnscheduledPrice,
            EstimatedUnscheduledCo2Emissions = planDTO.EstimatedUnscheduledCo2Emission
            
        };

        _db.Plan.Add(plan);

        await _db.SaveChangesAsync();

        return new PlanDTO
        {
            UserDeviceId = plan.UserDeviceId,
            StartTime = plan.PlanStartTime,
            EndTime = plan.PlanEndTime,
            EstimatedPrice = plan.EstimatedPrice,
            EstimatedCo2Emission = plan.EstimatedCo2Emissions,
            EstimatedUnscheduledPrice = plan.EstimatedUnscheduledPrice,
            EstimatedUnscheduledCo2Emission = plan.EstimatedUnscheduledCo2Emissions
        
        };
        
    }

}
