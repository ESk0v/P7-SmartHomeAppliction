using Hangfire.Annotations;
using Microsoft.AspNetCore.SignalR;

namespace Utilities.Models;

public class DevicePresetDto
{
    public string deviceId { get; set; } = "";
    public PresetDto preset { get; set;} = default!;
}