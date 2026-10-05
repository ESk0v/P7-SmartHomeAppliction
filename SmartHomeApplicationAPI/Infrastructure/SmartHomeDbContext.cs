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

    public virtual DbSet<DayAheadPrice> DayAheadPrices { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scans the assembly for all classes that implement IEntityTypeConfiguration<T> and applies any pending migrations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartHomeDbContext).Assembly);
    }

}
