using Utilities.Models;

namespace Shared.Dashboard;

public static class ScheduleIcons
{
    public const string Price = "bi-cash";
    public const string Co2 = "bi-cloud";

    private static readonly Dictionary<DeviceIcon, string> Icons = new()
    {
        [DeviceIcon.default_icon]   = "bi-plug-fill",
        [DeviceIcon.Dishwasher]     = "bi-droplet-fill",
        [DeviceIcon.WashingMachine] = "bi-bucket-fill",
        [DeviceIcon.Dryer]          = "bi-wind",
        [DeviceIcon.EvCharger]      = "bi-ev-station",
    };

    public static string Get(string? name) =>
        Enum.TryParse<DeviceIcon>(name, ignoreCase: true, out var icon)
        && Icons.TryGetValue(icon, out var value)
            ? value
            : Icons[DeviceIcon.default_icon];
}
