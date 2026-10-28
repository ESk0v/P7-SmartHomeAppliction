using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;

public interface IOptimizationService
{
    Task<OptimizationResultDTO> OptimizeAsync(OptimizationRequestDTO request);
    
}