namespace Utilities.Models;

public class UnscheduledEstimateRequestDto
{
    public string AreaCode { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal EnergyInKwh { get; set; }
}
