using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository;

public class ForecastingRepository : IForecastingRepository
{
    private readonly SmartHomeDbContext _db;

    public ForecastingRepository(SmartHomeDbContext db)
    {
        _db = db;
    }

    // Method implementation here
}