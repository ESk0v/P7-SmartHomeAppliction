using Utilities.Models;

namespace Shared.Dashboard;

public static class ScheduleIcons
{
    private static readonly Dictionary<DeviceIcon, string> Icons = new()
    {
        [DeviceIcon.default_icon]   = "bi-x-circle-fill text-danger",
        [DeviceIcon.Dishwasher]     = "bi-droplet",
        [DeviceIcon.WashingMachine] = "bi-bucket",
        [DeviceIcon.Dryer]          = "bi-wind",
        [DeviceIcon.EvCharger]      = "bi-ev-station",
    };

    public static string Get(string? name) =>
        Enum.TryParse<DeviceIcon>(name, ignoreCase: true, out var icon)
        && Icons.TryGetValue(icon, out var value)
            ? value
            : Icons[DeviceIcon.default_icon];
}
