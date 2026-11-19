using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHomeApplicationAPI.Infrastructure;

namespace SmartHomeApplicationAPI.Tests.Service;

public class UserPriceAreaMigrationTests
{
    [Fact]
    public void Migration_BackfillsDk1_WithoutADefaultForFutureUsers()
    {
        // SQL generation and model comparison do not open a database connection.
        using var db = new SmartHomeDbContext(new DbContextOptionsBuilder<SmartHomeDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused").Options);

        string sql = db.GetService<IMigrator>().GenerateScript(
            "20261106133625_AddBatteryCapacityAndNullableDuration", "20261118150000_AddUserPriceArea");

        Assert.Contains("ADD \"PriceArea\" character varying(3) NOT NULL DEFAULT 'DK1'", sql);
        Assert.Contains("ALTER COLUMN \"PriceArea\" DROP DEFAULT", sql);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
