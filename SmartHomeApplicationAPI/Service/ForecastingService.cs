using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;
namespace SmartHomeApplicationAPI.Service;

public class ForecastingService : IForecastingService
{
    private readonly IForecastingRepository _repository;

    public ForecastingService(IForecastingRepository repository)
    {
        _repository = repository;
    }

    // Method implemtentation here

    // Method will be something calling _repository.<NameHere>
}