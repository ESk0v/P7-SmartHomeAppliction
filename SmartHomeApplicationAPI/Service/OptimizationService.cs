using Utilities.Models;
namespace SmartHomeApplicationAPI.Service;

public class OptimizationService : IOptimizationService
{
    public async Task<OptimizationResultDTO> OptimizeAsync(OptimizationRequestDTO request)
    {
        var result = new OptimizationResultDTO(
            request.StartTime,
            request.EndTime,
            8.5m, 
            20
        );

        return result;

    }

}