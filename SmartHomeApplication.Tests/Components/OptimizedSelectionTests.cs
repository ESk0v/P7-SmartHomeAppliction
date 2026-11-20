using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartHomeApplication.Components.Pages;
using SmartHomeApplication.State;
using Utilities.Models;

namespace SmartHomeApplication.Tests.Components;

public class OptimizedSelectionTests
{
    [Fact]
    public void RefreshAfterDeletion_SelectsAnotherPlan_AndClearsThePickerAfterTheLastPlan()
    {
        var page = new Optimized();
        var state = new OptimizedPlanState { SelectedPlanId = 1 };
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        typeof(Optimized).GetProperty("PlanState", flags)!.SetValue(page, state);
        typeof(Optimized).GetProperty("Logger", flags)!.SetValue(page, NullLogger<Optimized>.Instance);
        var schedules = typeof(Optimized).GetField("allSchedules", flags)!;
        var refresh = typeof(Optimized).GetMethod("BuildPlansFromSchedules", flags)!;
        schedules.SetValue(page, new List<PlanDto> { Plan(1, 2), Plan(2), Plan(3) });
        refresh.Invoke(page, null);

        schedules.SetValue(page, new List<PlanDto> { Plan(3) });
        refresh.Invoke(page, null);
        Assert.Equal(3, state.SelectedPlanId);

        schedules.SetValue(page, new List<PlanDto>());
        refresh.Invoke(page, null);
        Assert.Equal(0, state.SelectedPlanId);
        foreach (string name in new[] { "plans", "plansById", "selectedSchedules" })
            Assert.Empty((System.Collections.IEnumerable)typeof(Optimized).GetField(name, flags)!.GetValue(page)!);
    }

    private static PlanDto Plan(int id, int? next = null) => new(id, next, $"Plan {id}", "", "#0a66c3",
        "Device", "Interruptible", "Cheapest", 80, null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1),
        DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 1m, 10m, 2m, 20m, 80m);
}
