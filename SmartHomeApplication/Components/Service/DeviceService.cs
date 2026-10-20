using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using Utilities.Models;

namespace SmartHomeApplication.Service
{
    public sealed class DeviceService(
        HttpClient httpClient,
        AuthenticationStateProvider authenticationStateProvider) : IDeviceService
    {
        public async Task CreateDeviceAsync(DevicesDto deviceDto, CancellationToken cancellationToken = default)
        {
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();

            var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var request = new DevicesDto
            {
                UserId = userId,
                Model = deviceDto.Model,
                Category = deviceDto.Category,
                Preset = deviceDto.Preset,
                ConsumptionInKwh = deviceDto.ConsumptionInKwh,
                DurationInMinutes = deviceDto.DurationInMinutes
            };

            using var response = await httpClient.PostAsJsonAsync(
                "api/device-controller/create-device",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Error creating device: {response.StatusCode}, {errorContent}",
                    null,
                    response.StatusCode);
            }
        }
    }
}
