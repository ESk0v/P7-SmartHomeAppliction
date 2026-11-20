using System.ComponentModel.DataAnnotations;

namespace Utilities.Models;

public class SaveGeneratedPlanRequestDto
{
    // Null creates a new plan; otherwise only unfinished blocks of this plan are replaced.
    public int? ReplacesPlanId { get; set; }
    public int UserDeviceId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string AreaCode { get; set; } = string.Empty;
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public string OptimizationObjective { get; set; } = string.Empty;
    public int? PresetId { get; set; }
    public int? ChargeStopPercentage { get; set; }

    [Required, MinLength(1)]
    public List<OptimizationResultDTO> Intervals { get; set; } = new();
}
