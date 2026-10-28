using System.ComponentModel.DataAnnotations;

namespace Utilities.Models
{
    public class PresetDto
    {
        public int id { get; set; }
        public string presetName { get; set; } = string.Empty;
        public decimal consumption { get; set; }
        public int duration { get; set; }
    }
}

