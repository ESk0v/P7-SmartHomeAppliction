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
    public async Task SaveSplitPlan_SendsAllIntervalsAndSettingsInOneRequest()
    {
        SaveGeneratedPlanRequestDto? saved = null;
        int requests = 0;
        using var handler = new StubHandler(async request =>
        {
            requests++;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("save-generated-plan", request.RequestUri!.AbsolutePath);
            Assert.Equal("test-user", Assert.Single(request.Headers.GetValues("UserId")));
            saved = await request.Content!.ReadFromJsonAsync<SaveGeneratedPlanRequestDto>();
            return Json(new PlanDTO { planId = 3 });
        });
        var form = CreateForm(handler);
        Set(form, "area", "DK2");
        var intervals = new List<OptimizationResultDTO>
        {
            new(WindowStart.AddHours(1), WindowStart.AddHours(1).AddMinutes(15), 1m, 2m),
            new(WindowStart.AddHours(3), WindowStart.AddHours(3).AddMinutes(30), 3m, 4m),
            new(WindowStart.AddHours(6), WindowStart.AddHours(6).AddMinutes(45), 5m, 6m)
        };
        Set(form, "optimizedIntervals", intervals);
        var expectedIntervals = intervals.ToArray();

        await Invoke(form, "SaveGeneratedPlanAsync");

        Assert.Equal(1, requests);
        Assert.NotNull(saved);
        Assert.Null(saved.ReplacesPlanId);
        Assert.Equal("DK2", saved.AreaCode);
        Assert.Equal(1, saved.UserDeviceId);
        Assert.Equal(WindowStart, saved.WindowStart);
        Assert.Equal(WindowStart.AddHours(8), saved.WindowEnd);
        Assert.Equal("Test plan", saved.PlanName);
        Assert.Equal(50, saved.ChargeStopPercentage);
        Assert.Equal(expectedIntervals, saved.Intervals);
        Assert.Equal(3, Property<OptimizedPlanState>(form, "PlanState").SelectedPlanId);
        Assert.EndsWith("/optimized", Property<NavigationManager>(form, "NavigationManager").Uri);
    }

    [Fact]
    public async Task SaveAppliance_SendsPresetAndOmitsChargeTarget()
    {
        SaveGeneratedPlanRequestDto? saved = null;
        int requests = 0;
        using var handler = new StubHandler(async request =>
        {
            requests++;
            Assert.EndsWith("save-generated-plan", request.RequestUri!.AbsolutePath);
            saved = await request.Content!.ReadFromJsonAsync<SaveGeneratedPlanRequestDto>();
            return Json(new PlanDTO { planId = 10 });
        });
        var form = CreateForm(handler);
        Set(form, "userDevices", new List<PlanUserDeviceDto>
        {
            new(1, 1, "Test device", "", "", "Non-Interruptible", "", null)
        });
        Field<ScheduleForm>(form, "ScheduleForm").Preset = new PresetDto { id = 9, consumption = 2.5m };

        await Invoke(form, "SaveGeneratedPlanAsync");

        Assert.Equal(1, requests);
        Assert.NotNull(saved);
        Assert.Equal(9, saved.PresetId);
        Assert.Equal(1, saved.UserDeviceId);
        Assert.Single(saved.Intervals);
        Assert.Null(saved.ChargeStopPercentage);
    }

    [Fact]
    public async Task SavePlan_WhenRejected_KeepsResultAndDoesNotNavigate()
    {
        int requests = 0;
        using var handler = new StubHandler(request =>
        {
            requests++;
            Assert.EndsWith("save-generated-plan", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("No electricity data is available.")
            });
        });
        var form = CreateForm(handler);

        await Invoke(form, "SaveGeneratedPlanAsync");

        Assert.Equal(1, requests);
        Assert.Contains("No electricity data", Field<string>(form, "schedulingError"));
        Assert.Single(Field<List<OptimizationResultDTO>>(form, "optimizedIntervals"));
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
        typeof(PlanForm).GetProperty(nameof(PlanForm.IsEditMode))!.SetValue(form, true);
        await Invoke(form, "SubmitPlan");

        Assert.NotNull(baseline);
        Assert.Equal(start, baseline.StartTime);
        Assert.Equal(start.AddMinutes(90), baseline.EndTime);
        Assert.Equal(24m, baseline.EnergyInKwh); // 80 kWh, charging from the assumed 20% to 50%.
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
        using var handler = new StubHandler(async request =>
        {
            calls++;
            Assert.EndsWith("/optimize", request.RequestUri!.AbsolutePath);
            Assert.Equal("DK2", (await request.Content!.ReadFromJsonAsync<OptimizationRequestDTO>())!.Area);
            return calls == 1
                ? Json(new[]
                {
                    new OptimizationResultDTO(start.AddHours(2), start.AddHours(3), 2m, 20m),
                    new OptimizationResultDTO(start, start.AddHours(1), 1m, 10m)
                })
                : new HttpResponseMessage(HttpStatusCode.BadRequest);
        });
        var form = CreateForm(handler);
        Set(form, "area", "DK2");
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

    [Fact]
    public async Task RegenerateEdit_PreviewsReplacement_ThenSavesAllBlocksInOneRequest()
    {
        SaveGeneratedPlanRequestDto? saved = null;
        var events = new List<string>();
        using var handler = new StubHandler(async request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            events.Add(request.Method.Method + " " + path);
            if (path.EndsWith("/optimize")) return Json(RegeneratedIntervals());
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("save-generated-plan", path);
            Assert.Equal("test-user", Assert.Single(request.Headers.GetValues("UserId")));
            saved = await request.Content!.ReadFromJsonAsync<SaveGeneratedPlanRequestDto>();
            return Json(new PlanDTO { planId = 101 });
        });
        var form = CreateEditingForm(handler);
        var unrelated = DisplayPlan(30, null);
        Set(form, "activeSchedules", new List<PlanDto> { DisplayPlan(10, 20), DisplayPlan(20, null), unrelated });

        await Invoke(form, "GenerateScheduling");

        Assert.Single(events); // Generating must not persist or delete anything.
        var preview = Property<List<PlanDto>>(form, "ChartSchedules");
        Assert.Equal(3, preview.Count);
        Assert.All(preview.Take(2), plan => Assert.True(plan.planId < 0));
        Assert.Same(unrelated, preview[2]);

        // Renaming is safe without running the optimizer again.
        Field<ScheduleForm>(form, "ScheduleForm").PlanName = "Replacement";
        await Invoke(form, "SubmitPlan");

        Assert.NotNull(saved);
        Assert.Equal(10, saved.ReplacesPlanId);
        Assert.Equal(RegeneratedIntervals(), saved.Intervals);
        Assert.Equal("Replacement", saved.PlanName);
        Assert.Equal(WindowStart, saved.WindowStart);
        Assert.Equal(WindowStart.AddHours(8), saved.WindowEnd);
        Assert.Equal(2, events.Count);
        Assert.EndsWith("save-generated-plan", events[^1]);
        Assert.Equal(101, Property<OptimizedPlanState>(form, "PlanState").SelectedPlanId);
        Assert.True(Property<OptimizedPlanState>(form, "PlanState").ReturnToSelectedPlan);
        Assert.EndsWith("/optimized", Property<NavigationManager>(form, "NavigationManager").Uri);
    }

    [Theory]
    [InlineData("window")]
    [InlineData("device")]
    [InlineData("preset")]
    [InlineData("charge")]
    [InlineData("priority")]
    public async Task RegenerateEdit_WhenSchedulingInputsChange_RequiresGenerationAgain(string input)
    {
        int requests = 0;
        using var handler = new StubHandler(request =>
        {
            requests++;
            Assert.EndsWith("/optimize", request.RequestUri!.AbsolutePath);
            return Task.FromResult(Json(RegeneratedIntervals()));
        });
        var form = CreateEditingForm(handler);
        await Invoke(form, "GenerateScheduling");
        var schedule = Field<ScheduleForm>(form, "ScheduleForm");
        switch (input)
        {
            case "window": schedule.Start += 15; break;
            case "device": Set(form, "selectedDeviceId", "2"); break;
            case "preset": schedule.Preset = new PresetDto { id = 99 }; break;
            case "charge": schedule.ChargeStopPercentage = 80; break;
            case "priority": schedule.Priority = "Greenest"; break;
        }

        await Invoke(form, "SubmitPlan");

        Assert.Equal(1, requests);
        Mock.Get(Property<INotificationService>(form, "NotificationService")).Verify(
            notifications => notifications.WarningNotification(It.Is<string>(message => message.Contains("Generate the plan again"))), Times.Once);
    }

    [Fact]
    public async Task RegenerateEdit_WhenInputsChangeDuringTheRequest_RejectsTheStaleResult()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>();
        int requests = 0;
        using var handler = new StubHandler(request => { requests++; return response.Task; });
        var form = CreateEditingForm(handler);

        Task generating = Invoke(form, "GenerateScheduling");
        Assert.True(Field<bool>(form, "isGenerating"));
        await Invoke(form, "SubmitPlan");
        Field<ScheduleForm>(form, "ScheduleForm").Priority = "Greenest";
        response.SetResult(Json(RegeneratedIntervals()));
        await generating;
        await Invoke(form, "SubmitPlan");

        Assert.Equal(1, requests);
        Assert.False(Field<bool>(form, "isGenerating"));
        Mock.Get(Property<INotificationService>(form, "NotificationService")).Verify(
            notifications => notifications.WarningNotification(It.Is<string>(message => message.Contains("Generate the plan again"))), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegenerateEdit_WhenOptimizationFailsOrReturnsNoIntervals_PreservesOriginalPreview(bool emptyResult)
    {
        using var handler = new StubHandler(request => Task.FromResult(emptyResult
            ? Json(Array.Empty<OptimizationResultDTO>())
            : new HttpResponseMessage(HttpStatusCode.UnprocessableEntity) { Content = new StringContent("Choose a longer window.") }));
        var form = CreateEditingForm(handler);
        var originals = Field<List<OptimizationResultDTO>>(form, "optimizedIntervals").ToArray();

        await Invoke(form, "GenerateScheduling");

        Assert.Equal(originals, Field<List<OptimizationResultDTO>>(form, "optimizedIntervals"));
        Assert.True(Property<bool>(form, "displayPlan"));
        Assert.False(Field<bool>(form, "isGenerating"));
        Assert.Empty(Field<List<PlanDto>>(form, "previewSchedules"));
    }

    [Fact]
    public async Task RegenerateEdit_WhenReplacementFails_DoesNotSaveSeparateBlocksOrNavigateAway()
    {
        int saves = 0;
        using var handler = new StubHandler(request =>
        {
            Assert.NotEqual(HttpMethod.Delete, request.Method);
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/optimize")) return Task.FromResult(Json(RegeneratedIntervals()));
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("save-generated-plan", path);
            saves++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                { Content = new StringContent("Replacement was not saved.") });
        });
        var form = CreateEditingForm(handler);

        await Invoke(form, "GenerateScheduling");
        await Invoke(form, "SubmitPlan");

        Assert.Equal(1, saves);
        Assert.Contains("Replacement was not saved", Field<string>(form, "schedulingError"));
        Assert.False(Field<bool>(form, "isSaving"));
        Assert.Equal("https://localhost/", Property<NavigationManager>(form, "NavigationManager").Uri);
    }

    [Fact]
    public async Task RegenerateEdit_DoubleClickingUpdate_SendsOnlyOneReplacement()
    {
        int saves = 0;
        var response = new TaskCompletionSource<HttpResponseMessage>();
        using var handler = new StubHandler(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/optimize")) return Task.FromResult(Json(RegeneratedIntervals()));
            Assert.EndsWith("save-generated-plan", path);
            saves++;
            return response.Task;
        });
        var form = CreateEditingForm(handler);
        await Invoke(form, "GenerateScheduling");

        Task saving = Invoke(form, "SubmitPlan");
        Assert.True(Field<bool>(form, "isSaving"));
        await Invoke(form, "SubmitPlan");
        response.SetResult(Json(new PlanDTO { planId = 101 }));
        await saving;

        Assert.Equal(1, saves);
        Assert.False(Field<bool>(form, "isSaving"));
    }

    [Fact]
    public async Task CancelGeneratedPlan_DiscardsOnlyTheResult_AndKeepsAllFormChoices()
    {
        using var handler = new StubHandler(request => Task.FromResult(Json(RegeneratedIntervals())));
        var form = CreateEditingForm(handler);
        typeof(PlanForm).GetProperty(nameof(PlanForm.IsEditMode))!.SetValue(form, false);
        var schedule = Field<ScheduleForm>(form, "ScheduleForm");
        var choices = (schedule.PlanName, schedule.Start, schedule.End, schedule.ChargeStopPercentage, schedule.Priority);
        var preset = schedule.Preset;
        await Invoke(form, "GenerateScheduling");

        typeof(PlanForm).GetMethod("CancelGeneratedPlan", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null);

        Assert.Same(schedule, Field<ScheduleForm>(form, "ScheduleForm"));
        Assert.Equal(choices, (schedule.PlanName, schedule.Start, schedule.End, schedule.ChargeStopPercentage, schedule.Priority));
        Assert.Same(preset, schedule.Preset);
        Assert.Equal("1", Field<string>(form, "selectedDeviceId"));
        Assert.Empty(Field<List<OptimizationResultDTO>>(form, "optimizedIntervals"));
        Assert.Empty(Field<List<PlanDto>>(form, "previewSchedules"));
        Assert.Null(Field<DateTime?>(form, "requestedWindowStart"));
        Assert.Null(Field<DateTime?>(form, "requestedWindowEnd"));
        Assert.Null(Field<object?>(form, "generatedFor"));
        Assert.False(Property<bool>(form, "displayPlan"));
    }

    [Theory]
    [InlineData("GenerateScheduling")]
    [InlineData("UpdatePlanAsync")]
    public async Task GenerationAndEditing_RejectNonexistentCopenhagenTimes_WithoutSendingRequests(string action)
    {
        int requests = 0;
        using var handler = new StubHandler(request =>
        {
            requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        });
        var form = CreateEditingForm(handler);
        Set(form, "schedulingDate", new DateTime(2026, 3, 29));
        var schedule = Field<ScheduleForm>(form, "ScheduleForm");
        schedule.Start = 150; // 02:30 does not exist when Copenhagen advances to summer time.
        schedule.End = 240;

        await Invoke(form, action);

        Assert.Equal(0, requests);
        Assert.Equal(150, schedule.Start);
        Assert.Equal(240, schedule.End);
        Mock.Get(Property<INotificationService>(form, "NotificationService")).Verify(
            notifications => notifications.ErrorNotification(It.Is<string>(message => message.Contains("does not exist"))), Times.Once);
    }

    private static List<OptimizationResultDTO> RegeneratedIntervals() => new()
    {
        new(WindowStart.AddHours(1), WindowStart.AddHours(1).AddMinutes(15), 1m, 10m),
        new(WindowStart.AddHours(3), WindowStart.AddHours(3).AddMinutes(45), 3m, 30m)
    };

    private static PlanForm CreateEditingForm(HttpMessageHandler handler)
    {
        var form = CreateForm(handler);
        typeof(PlanForm).GetProperty(nameof(PlanForm.IsEditMode))!.SetValue(form, true);
        Set(form, "editingPlanId", 10);
        Set(form, "editLoaded", true);
        Set(form, "userId", "test-user");
        Set(form, "schedulingDate", WindowStart.Date);
        var schedule = Field<ScheduleForm>(form, "ScheduleForm");
        schedule.Start = 7 * 60; // Copenhagen UTC+1 in November.
        schedule.End = 15 * 60;
        schedule.Priority = "Cheapest";
        var plan = ExistingPlan(10, 20, WindowStart.AddHours(1), WindowStart.AddHours(2));
        var next = ExistingPlan(20, null, WindowStart.AddHours(3), WindowStart.AddHours(4));
        Set(form, "existingPlan", plan);
        Set(form, "existingPlanIntervals", new List<PlanDTO> { plan, next });
        Set(form, "optimizedIntervals", new List<OptimizationResultDTO>
        {
            new(plan.startTime, plan.endTime, plan.estimatedCost, plan.estimatedCo2Emissions),
            new(next.startTime, next.endTime, next.estimatedCost, next.estimatedCo2Emissions)
        });
        return form;
    }

    private static PlanDto DisplayPlan(int id, int? nextId) => new(
        id, nextId, "Saved plan", "", "#111111", "Saved device", "Interruptible", "Cheapest",
        50, null, WindowStart, WindowStart.AddHours(1), WindowStart, WindowStart.AddHours(8), 1m, 10m, 0m, 0m, 80m);

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
        Set(form, "userId", "test-user");
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
