using Microsoft.AspNetCore.Mvc;
using Utilities.Models;
using SmartHomeApplicationAPI.Service;

namespace Controller;

[ApiController]
[Route("api/device-controller")]
public class DeviceController : ControllerBase
{
    public DeviceController() {  }

    [HttpGet("device")]
    public async Task<string> getDevice()
    {
        return "";
    }
}