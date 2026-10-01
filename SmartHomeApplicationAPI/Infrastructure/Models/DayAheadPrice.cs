using System;
using System.Collections.Generic;

namespace SmartHomeApplicationAPI.Infrastructure.Models;

public partial class DayAheadPrice
{
    public DateTime Time { get; set; }

    public string PriceArea { get; set; } = null!;

    public decimal Price { get; set; }

    public bool IsPredicted { get; set; }
}
