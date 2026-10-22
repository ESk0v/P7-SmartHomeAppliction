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
            .Select(p => new PresetDto(
                p.Id,
                p.PresetName,
                p.ConsumptionInKwh,
                p.DurationInMinutes
            ))
            .ToListAsync();
        
    }

    public async Task<List<UserDeviceDto>> GetUserDevicesAsync(string userId)
    {
        return await _db.UserDevice
            .AsNoTracking()
            .Where(ud => ud.UserId == userId)
            .Join(_db.Device,
                ud => ud.DeviceId,
                d => d.Id,
                (ud, d) => new UserDeviceDto(
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
            UserDeviceId = planDTO.UserDeviceId,
            StartTime = planDTO.StartTime,
            EndTime = planDTO.EndTime,
            EstimatedPrice = planDTO.EstimatedPrice,
            EstimatedCo2Emissions = planDTO.EstimatedCO2Emission
        };

        _db.Plan.Add(plan);

        await _db.SaveChangesAsync();

        return new PlanDTO
        {
            UserDeviceId = plan.UserDeviceId,
            StartTime = plan.StartTime,
            EndTime = plan.EndTime,
            EstimatedPrice = plan.EstimatedPrice,
            EstimatedCO2Emission = plan.EstimatedCo2Emissions
        
        };
        
    }

}