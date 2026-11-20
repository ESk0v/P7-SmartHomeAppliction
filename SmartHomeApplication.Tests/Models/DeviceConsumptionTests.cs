using System.ComponentModel.DataAnnotations;
using Utilities.Models;

namespace SmartHomeApplication.Tests.Models;

public class DeviceConsumptionTests
{
    [Theory]
    [InlineData("Non-Interruptible", 10, true)]
    [InlineData("Non-Interruptible", 10.01, false)]
    [InlineData("Interruptible", 11, true)]
    public void CreateAndUpdate_EnforceApplianceLimit_WithoutRestrictingEvPower(string category, double consumption, bool expected)
    {
        var capacity = category == "Interruptible" ? (decimal?)80m : null;
        Assert.Equal(expected, IsValid(new CreateDeviceDto
        {
            Model = "Test device", Category = category, Preset = "Test",
            ConsumptionInKwh = (decimal)consumption, DurationInMinutes = 60,
            BatteryCapacityInKwh = capacity
        }));
        Assert.Equal(expected, IsValid(new UpdateUserDeviceDto
        {
            Nickname = "Test device", Category = category,
            ConsumptionInKwh = (decimal)consumption, BatteryCapacityInKwh = capacity
        }));
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(10.01, false)]
    public void AppliancePreset_EnforcesTenKwhLimit(double consumption, bool expected) =>
        Assert.Equal(expected, IsValid(new PresetDeviceDto
        {
            PresetName = "Test", ConsumptionInKwh = (decimal)consumption, DurationInMinutes = 60
        }));

    [Fact]
    public void EvCreation_StillRequiresBatteryCapacity() => Assert.False(IsValid(new CreateDeviceDto
    {
        Model = "EV", Category = "Interruptible", Preset = "Info", ConsumptionInKwh = 11m
    }));

    private static bool IsValid(object model) =>
        Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), validateAllProperties: true);
}
