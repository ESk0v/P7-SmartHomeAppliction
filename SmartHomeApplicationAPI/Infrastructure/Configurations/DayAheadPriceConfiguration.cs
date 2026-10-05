using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Infrastructure.Configurations
{
    public class DayAheadPriceConfiguration : IEntityTypeConfiguration<DayAheadPrice>
    {
        public void Configure(EntityTypeBuilder<DayAheadPrice> builder)
        {
            builder.HasKey(k => k.Id).HasName("day_ahead_price_pkey");
            builder.ToTable("day_ahead_price");
            builder.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            builder.Property(e => e.IsPredicted)
                .HasDefaultValue(false)
                .HasColumnName("is_predicted");
            builder.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            builder.Property(e => e.PriceArea)
                .HasMaxLength(3)
                .HasColumnName("price_area");
            builder.Property(e => e.Time)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("time");
        }
    }
}
