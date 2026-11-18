using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;

namespace SmartHomeApplicationAPI.Tests.Service;

public class PlanDeletionTests
{
    private static readonly DateTime Now = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Delete_UnfinishedBlocks_PreservesHistoryAndReconnectsSurvivors()
    {
        using var db = CreateDb();
        var device = Device(1, "owner");
        db.Plan.AddRange(
            Block(1, device, Now.AddHours(-2), 2),
            Block(2, device, Now.AddHours(1), 3),
            Block(3, device, Now.AddHours(-1), 4),
            Block(4, device, Now.AddHours(2)),
            Block(5, device, Now.AddHours(3)));
        await db.SaveChangesAsync();

        Assert.True(await CreateService(db).DeletePlanAsync("owner", 2));

        db.ChangeTracker.Clear();
        var remaining = await db.Plan.OrderBy(plan => plan.Id).ToListAsync();
        Assert.Equal(new[] { 1, 3, 5 }, remaining.Select(plan => plan.Id));
        Assert.Equal(3, remaining[0].NextPlanId);
        Assert.Null(remaining[1].NextPlanId);
        Assert.Equal(1m, remaining[0].EstimatedPrice);
        Assert.Equal(10m, remaining[0].EstimatedCo2Emissions);
    }

    [Fact]
    public async Task Delete_CannotStartAtAnotherUsersPlan()
    {
        using var db = CreateDb();
        db.Plan.Add(Block(1, Device(1, "other-user"), Now.AddHours(1)));
        await db.SaveChangesAsync();

        Assert.False(await CreateService(db).DeletePlanAsync("owner", 1));
        Assert.Equal(1, await db.Plan.CountAsync());
    }

    [Fact]
    public async Task Delete_DoesNotFollowLinksIntoAnotherUsersPlans()
    {
        using var db = CreateDb();
        db.Plan.AddRange(
            Block(1, Device(1, "owner"), Now.AddHours(1), 2),
            Block(2, Device(2, "other-user"), Now.AddHours(2)));
        await db.SaveChangesAsync();

        Assert.True(await CreateService(db).DeletePlanAsync("owner", 1));
        Assert.Equal(2, (await db.Plan.SingleAsync()).Id);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Delete_UsesTheEndTimeToDetermineWhetherABlockIsUnfinished(int endOffsetSeconds, bool shouldDelete)
    {
        using var db = CreateDb();
        db.Plan.Add(Block(1, Device(1, "owner"), Now.AddSeconds(endOffsetSeconds)));
        await db.SaveChangesAsync();

        Assert.True(await CreateService(db).DeletePlanAsync("owner", 1));
        Assert.Equal(shouldDelete ? 0 : 1, await db.Plan.CountAsync());
    }

    private static SmartHomeDbContext CreateDb() => new(new DbContextOptionsBuilder<SmartHomeDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static PlanService CreateService(SmartHomeDbContext db) =>
        new(new PlanRepository(db), new FixedTimeProvider());

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private static UserDevice Device(int id, string owner) => new()
    {
        Id = id, DeviceId = id, UserId = owner, Nickname = "Test device"
    };

    private static Plan Block(int id, UserDevice device, DateTime end, int? next = null) => new()
    {
        Id = id, Name = $"Block {id}", UserDeviceId = device.Id, UserDevice = device,
        NextPlanId = next, PlanStartTime = end.AddHours(-1), PlanEndTime = end,
        WindowStartTime = end.AddHours(-2), WindowEndTime = end,
        OptimizationObjective = "Cheapest", EstimatedPrice = 1m, EstimatedCo2Emissions = 10m
    };
}
