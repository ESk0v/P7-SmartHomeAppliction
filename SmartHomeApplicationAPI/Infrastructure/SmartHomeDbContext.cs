using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure.Models;

namespace SmartHomeApplicationAPI.Infrastructure;

public partial class SmartHomeDbContext : DbContext
{
    public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DayAheadPrice> DayAheadPrices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DayAheadPrice>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("day_ahead_price");

            entity.Property(e => e.IsPredicted)
                .HasDefaultValue(false)
                .HasColumnName("is_predicted");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.PriceArea)
                .HasMaxLength(3)
                .HasColumnName("price_area");
            entity.Property(e => e.Time)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("time");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
