using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartHomeApplication.Components.Shared.Demand;
using SmartHomeApplication.State;
using Utilities.Models;

namespace SmartHomeApplication.Tests.Components;

public class PlanFormTests
{
    private static readonly DateTime WindowStart = new(2026, 11, 20, 6, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SaveSplitPlan_ExcludesPauses_DistributesOneBaseline_AndPreservesLinks()
    {
        UnscheduledEstimateRequestDto? baseline = null;
        var saved = new List<PlanDTO>();
        using var handler = new StubHandler(async request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("unscheduled-estimate"))
            {
                Assert.Null(baseline);
                baseline = await request.Content!.ReadFromJsonAsync<UnscheduledEstimateRequestDto>();
                return Json(new UnscheduledEstimateDto(10m, 100m));
            }

            Assert.EndsWith("post-plan", request.RequestUri.AbsolutePath);
            var plan = (await request.Content!.ReadFromJsonAsync<PlanDTO>())!;
            plan.planId = saved.Count + 1;
            saved.Add(plan);
            return Json(plan);
        });
        var form = CreateForm(handler);
        Set(form, "optimizedIntervals", new List<OptimizationResultDTO>
        {
            new(WindowStart.AddHours(1), WindowStart.AddHours(1).AddMinutes(15), 1m, 2m),
            new(WindowStart.AddHours(3), WindowStart.AddHours(3).AddMinutes(30), 3m, 4m),
            new(WindowStart.AddHours(6), WindowStart.AddHours(6).AddMinutes(45), 5m, 6m)
        });

        await Invoke(form, "addPlan");

        Assert.NotNull(baseline);
        Assert.Equal(WindowStart, baseline.StartTime);
        Assert.Equal(WindowStart.AddMinutes(90), baseline.EndTime);
        Assert.Equal(40m, baseline.EnergyInKwh);
        Assert.Equal(3, saved.Count);
        Assert.Equal(new int?[] { null, 1, 2 }, saved.Select(plan => plan.nextPlanId));
        Assert.Equal(new[] { 6d, 3d, 1d }, saved.Select(plan => (plan.startTime - WindowStart).TotalHours));
        Assert.Equal(10m, saved.Sum(plan => plan.estimatedUnscheduledCost!.Value));
        Assert.Equal(100m, saved.Sum(plan => plan.estimatedUnscheduledCo2Emissions!.Value));
        Assert.Equal(5m, saved[0].estimatedUnscheduledCost);
        Assert.All(saved, plan =>
        {
            Assert.Equal(DateTimeKind.Utc, plan.startTime.Kind);
            Assert.Equal(WindowStart, plan.windowStartTime);
            Assert.Equal("Test plan", plan.planName);
        });
        Assert.Equal(3, Property<OptimizedPlanState>(form, "PlanState").SelectedPlanId);
        Assert.EndsWith("/optimized", Property<NavigationManager>(form, "NavigationManager").Uri);
    }

    [Fact]
    public async Task SaveAppliance_UsesPresetEnergy_AndKeepsEntireBaselineOnSingleInterval()
    {
        UnscheduledEstimateRequestDto? baseline = null;
        PlanDTO? saved = null;
        using var handler = new StubHandler(async request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("unscheduled-estimate"))
            {
                baseline = await request.Content!.ReadFromJsonAsync<UnscheduledEstimateRequestDto>();
                return Json(new UnscheduledEstimateDto(8m, 60m));
            }

            saved = (await request.Content!.ReadFromJsonAsync<PlanDTO>())!;
            saved.planId = 10;
            return Json(saved);
        });
        var form = CreateForm(handler);
        Set(form, "isInterruptible", false);
        Field<ScheduleForm>(form, "ScheduleForm").Preset = new PresetDto { id = 9, consumption = 2.5m };

        await Invoke(form, "addPlan");

        Assert.NotNull(baseline);
        Assert.Equal(2.5m, baseline.EnergyInKwh);
        Assert.Equal(WindowStart.AddMinutes(30), baseline.EndTime);
        Assert.NotNull(saved);
        Assert.Equal(8m, saved.estimatedUnscheduledCost);
        Assert.Equal(60m, saved.estimatedUnscheduledCo2Emissions);
        Assert.Null(saved.nextPlanId);
        Assert.Null(saved.chargeStopPercentage);
    }

    [Fact]
    public async Task SavePlan_WhenBaselineFails_DoesNotSaveAnyIntervals()
    {
        int requests = 0;
        using var handler = new StubHandler(request =>
        {
            requests++;
            Assert.EndsWith("unscheduled-estimate", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("No electricity data is available.")
            });
        });
        var form = CreateForm(handler);

        await Invoke(form, "addPlan");

        Assert.Equal(1, requests);
        Assert.Contains("No electricity data", Field<string>(form, "schedulingError"));
        Assert.Equal(0, Property<OptimizedPlanState>(form, "PlanState").SelectedPlanId);
        Assert.Equal("https://localhost/", Property<NavigationManager>(form, "NavigationManager").Uri);
    }

    [Theory]
    [InlineData(1, DateTimeKind.Utc, 0)]
    [InlineData(7, DateTimeKind.Utc, 0)]
    [InlineData(1, DateTimeKind.Local, 0)]
    [InlineData(7, DateTimeKind.Local, 0)]
    [InlineData(1, DateTimeKind.Unspecified, 0)]
    [InlineData(7, DateTimeKind.Unspecified, 0)]
    [InlineData(1, DateTimeKind.Utc, 1)]
    [InlineData(7, DateTimeKind.Utc, 1)]
    [InlineData(1, DateTimeKind.Unspecified, -1)]
    [InlineData(7, DateTimeKind.Unspecified, -1)]
    public async Task EditSplitPlan_LoadsWholeChain_AndUpdatesItsBaselineWithoutTimeShift(int month, DateTimeKind timestampKind, int dayOffset)
    {
        // Older saved windows can have minutes that are not a multiple of 15.
        int minute = month == 1 ? 30 : 37;
        DateTime start = new(2026, month, 10, 23, minute, 0, DateTimeKind.Utc);
        DateTime ApiTime(DateTime utc) => timestampKind switch
        {
            DateTimeKind.Unspecified => TimeZoneInfo.ConvertTimeFromUtc(utc,
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen")),
            DateTimeKind.Local => utc.ToLocalTime(),
            _ => utc
        };
        var plans = new Dictionary<int, PlanDTO>
        {
            [10] = ExistingPlan(10, 20, start.AddMinutes(15), start.AddMinutes(45)),
            [20] = ExistingPlan(20, null, start.AddHours(3), start.AddHours(4))
        };
        foreach (var plan in plans.Values)
        {
            plan.windowStartTime = ApiTime(start);
            plan.windowEndTime = ApiTime(start.AddHours(8));
            plan.startTime = ApiTime(plan.startTime);
            plan.endTime = ApiTime(plan.endTime);
        }
        UnscheduledEstimateRequestDto? baseline = null;
        var updated = new List<PlanDTO>();
        using var handler = new StubHandler(async request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                Assert.Equal("test-user", Assert.Single(request.Headers.GetValues("UserId")));
                int id = int.Parse(request.RequestUri!.Segments[^1]);
                return Json(plans[id]);
            }
            if (request.RequestUri!.AbsolutePath.EndsWith("unscheduled-estimate"))
            {
                Assert.Null(baseline);
                baseline = await request.Content!.ReadFromJsonAsync<UnscheduledEstimateRequestDto>();
                return Json(new UnscheduledEstimateDto(12m, 90m));
            }

            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("test-user", Assert.Single(request.Headers.GetValues("UserId")));
            var update = (await request.Content!.ReadFromJsonAsync<PlanDTO>())!;
            Assert.EndsWith($"update-plan/{update.planId}", request.RequestUri.AbsolutePath);
            updated.Add(update);
            var stored = plans[update.planId!.Value];
            stored.planName = update.planName;
            stored.windowStartTime = ApiTime(update.windowStartTime!.Value);
            stored.windowEndTime = ApiTime(update.windowEndTime!.Value);
            return Json(update);
        });
        var form = CreateForm(handler);
        Set(form, "editingPlanId", 10);
        Set(form, "userId", "test-user");
        DateTime localStart = TimeZoneInfo.ConvertTimeFromUtc(start,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen"));
        DateTime sliderDate = localStart.Date.AddDays(-dayOffset);
        Set(form, "schedulingDate", sliderDate);

        await Invoke(form, "LoadExistingPlanAsync");
        Assert.Equal(2, Field<List<OptimizationResultDTO>>(form, "optimizedIntervals").Count);
        Assert.Equal(start.AddMinutes(15), Field<List<OptimizationResultDTO>>(form, "optimizedIntervals")[0].StartTime);
        Assert.Equal(DateTimeKind.Utc, Field<List<OptimizationResultDTO>>(form, "optimizedIntervals")[0].StartTime.Kind);
        var schedule = Field<ScheduleForm>(form, "ScheduleForm");
        Assert.Equal((int)(localStart - sliderDate).TotalMinutes, schedule.Start);
        Assert.Equal(schedule.Start + 8 * 60, schedule.End);
        Assert.True(Field<int>(form, "MinMinutes") <= schedule.Start);
        Assert.True(Field<int>(form, "MaxMinutes") >= schedule.End);
        Assert.Equal(minute == 30 ? 15 : 1, Field<int>(form, "TimeStep"));
        var expectedTimes = (schedule.Start, schedule.End);
        schedule.PlanName = " Renamed plan ";
        await Invoke(form, "UpdatePlanAsync");

        Assert.NotNull(baseline);
        Assert.Equal(start, baseline.StartTime);
        Assert.Equal(start.AddMinutes(90), baseline.EndTime);
        Assert.Equal(40m, baseline.EnergyInKwh);
        Assert.Equal(new int?[] { 10, 20 }, updated.Select(plan => plan.planId));
        Assert.Equal(new decimal?[] { 4m, 8m }, updated.Select(plan => plan.estimatedUnscheduledCost));
        Assert.Equal(new decimal?[] { 30m, 60m }, updated.Select(plan => plan.estimatedUnscheduledCo2Emissions));
        Assert.All(updated, plan =>
        {
            Assert.Equal(start, plan.windowStartTime);
            Assert.Equal(start.AddHours(8), plan.windowEndTime);
            Assert.Equal(DateTimeKind.Utc, plan.windowStartTime!.Value.Kind);
            Assert.Equal("Renamed plan", plan.planName);
        });
        Assert.Equal(10, Property<OptimizedPlanState>(form, "PlanState").SelectedPlanId);
        Assert.True(Property<OptimizedPlanState>(form, "PlanState").ReturnToSelectedPlan);

        await Invoke(form, "LoadExistingPlanAsync");
        var reopened = Field<ScheduleForm>(form, "ScheduleForm");
        Assert.Equal(expectedTimes, (reopened.Start, reopened.End));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 2)]
    public async Task GeneratePlan_PreviewsLinkedIntervalsInLocalTime_WithoutSaving(int month, int utcOffset)
    {
        var start = new DateTime(2026, month, 10, 12, 0, 0, DateTimeKind.Utc);
        int calls = 0;
        using var handler = new StubHandler(request =>
        {
            calls++;
            Assert.EndsWith("/optimize", request.RequestUri!.AbsolutePath);
            return Task.FromResult(calls == 1
                ? Json(new[]
                {
                    new OptimizationResultDTO(start.AddHours(2), start.AddHours(3), 2m, 20m),
                    new OptimizationResultDTO(start, start.AddHours(1), 1m, 10m)
                })
                : new HttpResponseMessage(HttpStatusCode.BadRequest));
        });
        var form = CreateForm(handler);
        Set(form, "schedulingDate", start.Date);
        var schedule = Field<ScheduleForm>(form, "ScheduleForm");
        schedule.Start = 9 * 60;
        schedule.End = 21 * 60;
        var device = new PlanUserDeviceDto(1, 1, "Test device", "Car", "#0a66c3", "Interruptible", "EV", 80m);
        Set(form, "userDevices", new List<PlanUserDeviceDto> { device });
        var saved = new PlanDto(123, null, "Saved plan", "", "#111111", "Saved device", "Non-Interruptible",
            "Cheapest", null, 60, start, start.AddHours(1), start, start.AddHours(4), 1m, 10m, 0m, 0m, null);
        Set(form, "activeSchedules", new List<PlanDto> { saved });

        await Invoke(form, "GenerateScheduling");

        var chart = Property<List<PlanDto>>(form, "ChartSchedules");
        Assert.Equal(1, calls);
        Assert.Equal(3, chart.Count);
        Assert.Same(saved, chart[2]);
        Assert.Equal(start.AddHours(utcOffset), chart[0].startTime);
        Assert.Equal(start.AddHours(3 + utcOffset), chart[1].endTime);
        Assert.Equal(chart[1].planId, chart[0].nextPlanId);
        Assert.Null(chart[1].nextPlanId);
        Assert.All(chart.Take(2), interval =>
        {
            Assert.True(interval.planId < 0);
            Assert.Equal("Test plan (Preview)", interval.planName);
            Assert.Equal(device.color, interval.colorCode);
        });
        Assert.Equal(start, Field<List<OptimizationResultDTO>>(form, "optimizedIntervals")[0].StartTime);
        Assert.Equal(0, Property<OptimizedPlanState>(form, "PlanState").SelectedPlanId);

        // A failed regeneration must remove the previous preview while retaining saved schedules.
        await Invoke(form, "GenerateScheduling");
        Assert.Equal(2, calls);
        Assert.Same(saved, Assert.Single(Property<List<PlanDto>>(form, "ChartSchedules")));
    }

    private static PlanDTO ExistingPlan(int id, int? nextId, DateTime start, DateTime end) => new()
    {
        planId = id, nextPlanId = nextId, userDeviceId = 1, planName = "Test plan",
        startTime = start, endTime = end, chargeStopPercentage = 50, presetId = 9
    };

    private static PlanForm CreateForm(HttpMessageHandler handler)
    {
        var form = new PlanForm();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        typeof(PlanForm).GetProperty("Http", flags)!.SetValue(form, new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") });
        typeof(PlanForm).GetProperty("NavigationManager", flags)!.SetValue(form, new TestNavigationManager());
        typeof(PlanForm).GetProperty("Logger", flags)!.SetValue(form, NullLogger<PlanForm>.Instance);
        typeof(PlanForm).GetProperty("NotificationService", flags)!.SetValue(form, Mock.Of<INotificationService>());
        typeof(PlanForm).GetProperty("PlanState", flags)!.SetValue(form, new OptimizedPlanState());
        Set(form, "userDevices", new List<PlanUserDeviceDto> { new(1, 1, "Test device", "", "", "Interruptible", "", 80m) });
        Set(form, "selectedDeviceId", "1");
        Set(form, "isInterruptible", true);
        Set(form, "requestedWindowStart", (DateTime?)WindowStart);
        Set(form, "requestedWindowEnd", (DateTime?)WindowStart.AddHours(8));
        Set(form, "optimizedIntervals", new List<OptimizationResultDTO> { new(WindowStart.AddHours(1), WindowStart.AddHours(1).AddMinutes(30), 1m, 2m) });
        Set(form, "ScheduleForm", new ScheduleForm { DeviceName = "Test device", PlanName = " Test plan ", ChargeStopPercentage = 50 });
        return form;
    }

    // Exercise the component's HTTP workflow without a browser or new rendering dependencies.
    private static Task Invoke(PlanForm form, string method) =>
        (Task)typeof(PlanForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null)!;

    private static void Set<T>(PlanForm form, string name, T value) =>
        typeof(PlanForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(form, value);

    private static T Field<T>(PlanForm form, string name) =>
        (T)typeof(PlanForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;

    private static T Property<T>(PlanForm form, string name) =>
        (T)typeof(PlanForm).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(form)!;

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");
        protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();
    }
}
