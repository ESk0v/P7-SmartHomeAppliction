using System;
using System.Collections.Generic;

namespace SmartHomeApplicationAPI.Infrastructure.Models;

public partial class DayAheadPrice
{
    public int Id { get; set; }

    public DateTime Time { get; set; }

    public string PriceArea { get; set; } = null!;

    public decimal Price { get; set; }

    public bool IsPredicted { get; set; }
}
