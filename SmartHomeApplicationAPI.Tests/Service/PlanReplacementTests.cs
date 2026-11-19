using FluentValidation;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHomeApplicationAPI.Infrastructure;
using SmartHomeApplicationAPI.Infrastructure.Models;
using SmartHomeApplicationAPI.Repository;
using SmartHomeApplicationAPI.Service;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Tests.Service;

public class PlanReplacementTests
{
    private static readonly DateTime Now = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(null, 7)]
    [InlineData(1, 5)]
    public async Task Save_CreatesOrReplacesACompleteChain_InOneSave(int? originalId, int expectedCount)
    {
        var writes = new WriteInterceptor();
        using var db = CreateDb(writes);
        await Seed(db);
        writes.Calls = 0;

        var saved = await Service(db).SaveGeneratedPlanAsync("owner", Request(originalId));

        Assert.NotNull(saved);
        Assert.Equal(1, writes.Calls);
        db.ChangeTracker.Clear();
        var plans = await db.Plan.OrderBy(plan => plan.PlanStartTime).ToListAsync();
        Assert.Equal(expectedCount, plans.Count);
        if (originalId.HasValue) Assert.DoesNotContain(plans, plan => plan.Id is 2 or 3);
        else Assert.Contains(plans, plan => plan.Id == 2);
        var history = Assert.Single(plans, plan => plan.Id == 1);
        Assert.Equal(originalId.HasValue ? null : (int?)2, history.NextPlanId);
        Assert.Equal("Original", history.Name);
        Assert.Equal(7m, history.EstimatedPrice);
        Assert.Contains(plans, plan => plan.Id == 4);
        Assert.Contains(plans, plan => plan.Id == 5);
        var replacement = plans.Single(plan => plan.Id == saved.planId);
        Assert.Equal(Now.AddHours(2), replacement.PlanStartTime);
        var tail = plans.Single(plan => plan.Id == replacement.NextPlanId);
        Assert.Equal(Now.AddHours(4), tail.PlanStartTime);
        Assert.Null(tail.NextPlanId);
        Assert.Equal(10m, replacement.EstimatedUnscheduledPrice + tail.EstimatedUnscheduledPrice);
        Assert.Equal(100m, replacement.EstimatedUnscheduledCo2Emissions + tail.EstimatedUnscheduledCo2Emissions);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("window")]
    [InlineData("overlap")]
    [InlineData("nonutc")]
    [InlineData("preset")]
    [InlineData("device")]
    [InlineData("area")]
    [InlineData("window-order")]
    [InlineData("original")]
    public async Task Replace_WhenInvalid_LeavesOriginalsUntouched(string invalid)
    {
        using var db = CreateDb();
        await Seed(db);
        var request = Request();
        switch (invalid)
        {
            case "empty": request.Intervals.Clear(); break;
            case "window": request.Intervals[0] = request.Intervals[0] with { StartTime = Now.AddMinutes(15) }; break;
            case "overlap": request.Intervals[1] = request.Intervals[1] with { StartTime = request.Intervals[0].StartTime }; break;
            case "nonutc": request.Intervals[0] = request.Intervals[0] with { StartTime = DateTime.SpecifyKind(request.Intervals[0].StartTime, DateTimeKind.Unspecified) }; break;
            case "preset": request.PresetId = 99; break;
            case "device": request.UserDeviceId = 2; break; // Another user's device.
            case "area": request.AreaCode = "DK3"; break;
            case "window-order": request.WindowEnd = request.WindowStart; break;
            case "original": request.ReplacesPlanId = 0; break;
        }

        await Assert.ThrowsAsync<ValidationException>(() => Service(db).SaveGeneratedPlanAsync("owner", request));

        db.ChangeTracker.Clear();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, await db.Plan.OrderBy(plan => plan.Id).Select(plan => plan.Id).ToArrayAsync());
        Assert.Equal(2, (await db.Plan.SingleAsync(plan => plan.Id == 1)).NextPlanId);
    }

    [Fact]
    public async Task Replace_CannotReplaceAnotherUsersPlan()
    {
        using var db = CreateDb();
        await Seed(db);

        Assert.Null(await Service(db).SaveGeneratedPlanAsync("owner", Request(5)));
        Assert.Equal(5, await db.Plan.CountAsync());
    }

    [Fact]
    public async Task Replace_CannotReplaceACompletedPlan()
    {
        using var db = CreateDb();
        await Seed(db);
        var completed = await db.Plan.FindAsync(1);
        completed!.NextPlanId = null;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() => Service(db).SaveGeneratedPlanAsync("owner", Request()));
        Assert.Equal(5, await db.Plan.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task Save_WhenTheSingleSaveFails_PersistsNeitherNewBlocksNorDeletion(int? originalId)
    {
        var writes = new WriteInterceptor();
        using var db = CreateDb(writes);
        await Seed(db);
        writes.Calls = 0;
        writes.Fail = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).SaveGeneratedPlanAsync("owner", Request(originalId)));

        Assert.Equal(1, writes.Calls);
        db.ChangeTracker.Clear();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, await db.Plan.OrderBy(plan => plan.Id).Select(plan => plan.Id).ToArrayAsync());
        Assert.Equal(2, (await db.Plan.SingleAsync(plan => plan.Id == 1)).NextPlanId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task Save_UsesStoredEvCapacityAndCombinedDuration_DistributesBaselineAndLinksChronologically(int? originalId)
    {
        using var db = CreateDb();
        await Seed(db);
        var request = Request(originalId);
        request.AreaCode = "DK2";
        request.PlanName = " Replacement ";
        request.ChargeStopPercentage = 50;
        request.Intervals = new()
        {
            new(Now.AddHours(5), Now.AddHours(5).AddMinutes(45), 5m, 50m),
            new(Now.AddHours(2), Now.AddHours(2).AddMinutes(15), 1m, 10m),
            new(Now.AddHours(3), Now.AddHours(3).AddMinutes(30), 3m, 30m)
        };
        var prices = new Mock<IElectricityPriceService>(MockBehavior.Strict);
        prices.Setup(value => value.GetUnscheduledEstimateAsync(It.Is<UnscheduledEstimateRequestDto>(baseline =>
                baseline.AreaCode == "DK2" && baseline.StartTime == request.WindowStart &&
                baseline.EndTime == request.WindowStart.AddMinutes(90) && baseline.EnergyInKwh == 40m)))
            .ReturnsAsync(new UnscheduledEstimateResultDto(new UnscheduledEstimateDto(10m, 100m), null));

        var saved = await Service(db, prices).SaveGeneratedPlanAsync("owner", request);

        prices.VerifyAll();
        db.ChangeTracker.Clear();
        var chain = new List<Plan>();
        int? next = saved!.planId;
        while (next.HasValue)
        {
            var block = (await db.Plan.FindAsync(next.Value))!;
            chain.Add(block);
            next = block.NextPlanId;
        }
        Assert.Equal(new[] { 2d, 3d, 5d }, chain.Select(block => (block.PlanStartTime - Now).TotalHours));
        Assert.Equal(new[] { 1m, 3m, 5m }, chain.Select(block => block.EstimatedPrice));
        Assert.Equal(10m, chain.Sum(block => block.EstimatedUnscheduledPrice));
        Assert.Equal(100m, chain.Sum(block => block.EstimatedUnscheduledCo2Emissions));
        Assert.Equal(10m * 15m / 90m, chain[0].EstimatedUnscheduledPrice);
        Assert.Equal(10m * 30m / 90m, chain[1].EstimatedUnscheduledPrice);
        Assert.All(chain, block =>
        {
            Assert.Equal("Replacement", block.Name);
            Assert.Equal(request.WindowStart, block.WindowStartTime);
            Assert.Equal(request.WindowEnd, block.WindowEndTime);
            Assert.Equal(50, block.ChargeStopPercentage);
        });
    }

    [Fact]
    public async Task Save_ApplianceUsesStoredPresetConsumption_AndKeepsBaselineOnSingleBlock()
    {
        using var db = CreateDb();
        await Seed(db);
        (await db.Device.FindAsync(1))!.Category = "Non-Interruptible";
        (await db.Preset.FindAsync(9))!.ConsumptionInKwh = 2.5m;
        await db.SaveChangesAsync();
        var request = Request(null);
        request.ChargeStopPercentage = null;
        request.Intervals = new() { new(Now.AddHours(2), Now.AddHours(3), 3m, 30m) };
        var prices = new Mock<IElectricityPriceService>(MockBehavior.Strict);
        prices.Setup(value => value.GetUnscheduledEstimateAsync(It.Is<UnscheduledEstimateRequestDto>(baseline =>
                baseline.EnergyInKwh == 2.5m && baseline.StartTime == request.WindowStart &&
                baseline.EndTime == request.WindowStart.AddHours(1))))
            .ReturnsAsync(new UnscheduledEstimateResultDto(new UnscheduledEstimateDto(8m, 60m), null));

        var saved = await Service(db, prices).SaveGeneratedPlanAsync("owner", request);

        prices.VerifyAll();
        var block = (await db.Plan.FindAsync(saved!.planId))!;
        Assert.Equal(8m, block.EstimatedUnscheduledPrice);
        Assert.Equal(60m, block.EstimatedUnscheduledCo2Emissions);
        Assert.Null(block.NextPlanId);
        Assert.Null(block.ChargeStopPercentage);
        Assert.Equal(9, block.DevicePresetId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task Save_WhenBaselineUnavailable_DoesNotWriteOrAlterOriginals(int? originalId)
    {
        var writes = new WriteInterceptor();
        using var db = CreateDb(writes);
        await Seed(db);
        writes.Calls = 0;
        var prices = new Mock<IElectricityPriceService>();
        prices.Setup(value => value.GetUnscheduledEstimateAsync(It.IsAny<UnscheduledEstimateRequestDto>()))
            .ReturnsAsync(new UnscheduledEstimateResultDto(null, "No baseline data."));

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            Service(db, prices).SaveGeneratedPlanAsync("owner", Request(originalId)));

        Assert.Equal("No baseline data.", error.Message);
        Assert.Equal(0, writes.Calls);
        db.ChangeTracker.Clear();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, await db.Plan.OrderBy(plan => plan.Id).Select(plan => plan.Id).ToArrayAsync());
        Assert.Equal(2, (await db.Plan.FindAsync(1))!.NextPlanId);
    }

    private static SmartHomeDbContext CreateDb(WriteInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<SmartHomeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());
        if (interceptor is not null) options.AddInterceptors(interceptor);
        return new SmartHomeDbContext(options.Options);
    }

    private static async Task Seed(SmartHomeDbContext db)
    {
        db.Device.Add(new Device { Id = 1, Model = "Car", Category = "Interruptible" });
        var owner = new UserDevice { Id = 1, DeviceId = 1, UserId = "owner", Nickname = "Owner's car" };
        var other = new UserDevice { Id = 2, DeviceId = 1, UserId = "other", Nickname = "Other car" };
        db.Preset.Add(new DevicePreset { Id = 9, DeviceId = 1, PresetName = "Info", BatteryCapacityInKwh = 80m, ConsumptionInKwh = 11m });
        db.Plan.AddRange(Block(1, owner, -2, -1, 2), Block(2, owner, 1, 2, 3), Block(3, owner, 3, 4),
            Block(4, owner, 5, 6), Block(5, other, 1, 2));
        await db.SaveChangesAsync();
    }

    private static Plan Block(int id, UserDevice device, int start, int end, int? next = null) => new()
    {
        Id = id, Name = "Original", UserDevice = device, UserDeviceId = device.Id,
        DevicePresetId = 9, PlanStartTime = Now.AddHours(start), PlanEndTime = Now.AddHours(end),
        WindowStartTime = Now, WindowEndTime = Now.AddHours(8), NextPlanId = next,
        EstimatedPrice = 7m, OptimizationObjective = "Cheapest"
    };

    private static SaveGeneratedPlanRequestDto Request(int? originalId = 1) => new()
    {
        ReplacesPlanId = originalId, UserDeviceId = 1, PlanName = "Replacement", PresetId = 9,
        ChargeStopPercentage = 80, OptimizationObjective = "Cheapest", AreaCode = "DK1",
        WindowStart = Now.AddHours(1), WindowEnd = Now.AddHours(6),
        Intervals = new[] { 2, 4 }.Select(hours => new OptimizationResultDTO(
            Now.AddHours(hours), Now.AddHours(hours + 1), 1m, 2m)).ToList()
    };

    private static PlanService Service(SmartHomeDbContext db, Mock<IElectricityPriceService>? prices = null)
    {
        if (prices is null)
        {
            prices = new Mock<IElectricityPriceService>();
            prices.Setup(value => value.GetUnscheduledEstimateAsync(It.IsAny<UnscheduledEstimateRequestDto>()))
                .ReturnsAsync(new UnscheduledEstimateResultDto(new UnscheduledEstimateDto(10m, 100m), null));
        }
        return new PlanService(new PlanRepository(db), prices.Object, new FixedTimeProvider());
    }
    private sealed class FixedTimeProvider : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }

    private sealed class WriteInterceptor : SaveChangesInterceptor
    {
        public int Calls;
        public bool Fail;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("Simulated database failure.");
            return ValueTask.FromResult(result);
        }
    }
}
