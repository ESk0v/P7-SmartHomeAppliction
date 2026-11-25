using System.Reflection;
using Microsoft.JSInterop;
using Moq;
using Shared.Dashboard;
using SmartHomeApplication.State;
using Utilities.Models;

namespace SmartHomeApplication.Tests.Components;

public class PriceChartTests
{
    private static readonly DateTime Hour = new(2026, 11, 20, 12, 0, 0);
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    [Fact]
    public async Task PlanSelection_OverridesTheMetricWithoutSaving_AndAllowsManualChanges()
    {
        var js = new Mock<IJSRuntime>();
        var preference = new ChartMetricPreference(js.Object);
        var chart = new PriceChart();
        typeof(PriceChart).GetProperty("MetricPreference", Flags)!.SetValue(chart, preference);

        SelectPlan(chart, 1, "Greenest");
        Call<object?>(chart, "OnParametersSet");
        Assert.Equal("g CO₂/kWh", Property<string>(chart, "Unit"));
        Assert.Empty(js.Invocations);

        await Call<Task>(chart, "ToggleMetric");
        Call<object?>(chart, "OnParametersSet");
        Assert.Equal("kr/kWh", Property<string>(chart, "Unit"));

        SelectPlan(chart, 2, "Greenest");
        Call<object?>(chart, "OnParametersSet");
        Assert.Equal("g CO₂/kWh", Property<string>(chart, "Unit"));

        SelectPlan(chart, 3, "Cheapest");
        Call<object?>(chart, "OnParametersSet");
        Assert.Equal("kr/kWh", Property<string>(chart, "Unit"));
        await Call<Task>(chart, "ToggleMetric");

        SelectPlan(chart, 4, "Cheapest");
        Call<object?>(chart, "OnParametersSet");
        Assert.Equal("kr/kWh", Property<string>(chart, "Unit"));
        Assert.True(await preference.GetAsync("anonymous"));
        Assert.Equal(2, js.Invocations.Count);
    }

    private static PlanDto Plan(int id, string objective) => new(id, null, $"Plan {id}", "", "#0a66c3",
        "Device", "Interruptible", objective, 80, null, Hour, Hour.AddHours(1),
        Hour, Hour.AddHours(2), 1m, 10m, 2m, 20m, 80m);

    private static void SelectPlan(PriceChart chart, int id, string objective) =>
        typeof(PriceChart).GetProperty(nameof(PriceChart.SelectedPlan))!.SetValue(chart, Plan(id, objective));

    [Fact]
    public void SwitchingMeasurements_KeepsIndependentFixedScalesAndTheSameHours()
    {
        var chart = new PriceChart();
        var points = new List<HourPriceDto> { new(Hour, -0.5m, true, 200m), new(Hour.AddHours(1), 9m) };
        Set(chart, "prices", points);

        Assert.Equal("kr/kWh", Property<string>(chart, "Unit"));
        Assert.Equal(-1m, Property<decimal>(chart, "MinValue"));
        Assert.Equal(7m, Property<decimal>(chart, "MaxValue"));

        Set(chart, "showCo2", true);

        Assert.Equal("g CO₂/kWh", Property<string>(chart, "Unit"));
        Assert.Equal(0m, Property<decimal>(chart, "MinValue"));
        Assert.Equal(400m, Property<decimal>(chart, "MaxValue"));
        Assert.Equal("height:50%", Call<string>(chart, "BarStyle", points[0]));
        Assert.Equal("top:50%", Call<string>(chart, "GridLineStyle", 200m));
        Assert.Same(points, typeof(PriceChart).GetField("prices", Flags)!.GetValue(chart));

        Set(chart, "showCo2", false);
        Assert.Equal(-1m, Property<decimal>(chart, "MinValue"));
        Assert.Equal(7m, Property<decimal>(chart, "MaxValue"));
    }

    [Fact]
    public void MissingCo2IsAGap_WhileZeroIsARealReading()
    {
        var chart = new PriceChart();
        Set(chart, "showCo2", true);
        var missing = new HourPriceDto(Hour, 3m, true);
        var zero = new HourPriceDto(Hour.AddHours(1), 3m, false, 0m);

        Assert.Equal("display:none", Call<string>(chart, "BarStyle", missing));
        Assert.Contains("CO₂ data unavailable", Call<string>(chart, "PointDescription", missing));
        Assert.Equal("height:0%;min-height:0", Call<string>(chart, "BarStyle", zero));
        Assert.Contains("0 g CO₂/kWh", Call<string>(chart, "ValueDescription", zero));
        Assert.DoesNotContain("Predicted price", Call<string>(chart, "ValueDescription", missing));
    }

    [Theory]
    [InlineData(false, 9, 100, "kr/kWh", "Predicted price")]
    [InlineData(true, 2, 628, "g CO₂/kWh", "CO₂ forecast")]
    public void ValuesAboveTheCap_AreClippedButRetainTheirExactValueInDescriptions(
        bool co2, int price, int emissions, string unit, string source)
    {
        var chart = new PriceChart();
        Set(chart, "showCo2", co2);
        var point = new HourPriceDto(Hour, price, true, emissions);

        Assert.Equal("height:100%", Call<string>(chart, "BarStyle", point));
        string description = Call<string>(chart, "ValueDescription", point);
        Assert.Contains($"{(co2 ? emissions : price)} {unit}", description);
        Assert.Contains(source, description);
        Assert.Contains("Above chart maximum", description);
    }

    private static void Set(PriceChart chart, string field, object value) =>
        typeof(PriceChart).GetField(field, Flags)!.SetValue(chart, value);

    private static T Property<T>(PriceChart chart, string property) =>
        (T)typeof(PriceChart).GetProperty(property, Flags)!.GetValue(chart)!;

    private static T Call<T>(PriceChart chart, string method, params object[] arguments) =>
        (T)typeof(PriceChart).GetMethod(method, Flags)!.Invoke(chart, arguments)!;
}
