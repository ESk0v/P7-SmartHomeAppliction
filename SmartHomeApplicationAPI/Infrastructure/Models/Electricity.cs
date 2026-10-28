
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeApplicationAPI.Infrastructure.Models
{
    [Table("electricity")]
    public class Electricity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("time", TypeName = "timestamp(0) without time zone")]
        public DateTime Time { get; set; }

        [Required]
        [Column("price_per_kwh", TypeName = "numeric(10,5)")]
        public decimal PricePerKwh { get; set; }

        [Required]
        [StringLength(3)]
        [Column("price_area")]
        public string PriceArea { get; set; } = null!;

        [Required]
        [Column("is_predicted")]
        public bool IsPredicted { get; set; }

        [Column("co2_emission_g_per_kwh", TypeName = "numeric(10,2)")]
        public decimal? Co2EmissionGramsPerKwh { get; set; }
    }
}
