namespace Utilities.Models
{
    public class PresetDeviceDto
    {
        public string PresetName { get; set; } = "";

        public decimal ConsumptionInKwh { get; set; }

        public int DurationInMinutes { get; set; }
    }
}