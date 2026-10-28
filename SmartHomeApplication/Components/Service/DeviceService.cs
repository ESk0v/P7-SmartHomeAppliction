using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using Utilities.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.VisualBasic;

namespace SmartHomeApplication.Service
{
    public class DeviceService(
        HttpClient httpClient,
        AuthenticationStateProvider authenticationStateProvider) : IDeviceService
    {
        public async Task CreateDeviceAsync(CreateDeviceDto deviceDto, CancellationToken cancellationToken = default)
        {
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();

            var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var request = new CreateDeviceDto
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

        public async Task<IEnumerable<CatalogueDeviceDto>> GetAllDevicesAsync(CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.GetAsync(
                $"api/device-controller/get-all-devices",
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Error retrieving devices: {response.StatusCode}, {errorContent}",
                    null,
                    response.StatusCode);
            }
            var devices = await response.Content.ReadFromJsonAsync<IEnumerable<CatalogueDeviceDto>>(cancellationToken: cancellationToken);
            return devices ?? Enumerable.Empty<CatalogueDeviceDto>();
        }

        public async Task<IEnumerable<UserDeviceDto>> GetUserDevicesAsync(CancellationToken cancellationToken = default)
        {
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();

            var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, "api/device-controller/get-userdevices");
            request.Headers.Add("User-Id", userId);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Error retrieving user devices: {response.StatusCode}, {errorContent}",
                    null,
                    response.StatusCode);
            }
            var userDevices = await response.Content.ReadFromJsonAsync<IEnumerable<UserDeviceDto>>(cancellationToken: cancellationToken);
            return userDevices ?? Enumerable.Empty<UserDeviceDto>();
        }

        public async Task UpdateUserDeviceAsync(UpdateUserDeviceDto UpdateUserDeviceDto, CancellationToken cancellationToken = default)
        {

            var response = await httpClient.PutAsJsonAsync(
                $"api/device-controller/update-userdevice",
                UpdateUserDeviceDto,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Error updating user device: {response.StatusCode}, {errorContent}",
                    null,
                    response.StatusCode);
            }

        }
    }
}
