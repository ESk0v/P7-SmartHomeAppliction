using System.ComponentModel.DataAnnotations;

namespace Utilities.Models;

public class OptimizationRequestDTO
{
    public string Area { get; set; } = string.Empty;

    public int UserDeviceId { get; set; }

    public int DevicePresetId { get; set; }

    public DateTime StartWindow { get; set; }

    public DateTime EndWindow { get; set; }

    public string Priority { get; set; } = string.Empty;
}
