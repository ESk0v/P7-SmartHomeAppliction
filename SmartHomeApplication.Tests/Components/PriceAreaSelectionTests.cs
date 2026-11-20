using System.Net;
using System.Reflection;
using System.Security.Claims;
using Utilities.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using SmartHomeApplication.Components.Pages;
using SmartHomeApplication.Components.Shared.Demand;
using Shared.Dashboard;

namespace SmartHomeApplication.Tests.Components;

public class PriceAreaSelectionTests
{
    [Theory]
    [InlineData("DK1", "DK1")]
    [InlineData("DK2", "DK2")]
    [InlineData(null, "DK1")] // Older sessions belong to users whose price area defaults to DK1.
    public async Task DashboardFormAndChart_UseTheSameSessionArea(string? claim, string expected)
    {
        var requests = new List<Uri>();
        using var handler = new StubHandler(requests);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var auth = new TestAuthenticationStateProvider(claim);
        var dashboard = Create<Dashboard>(http, auth);
        var form = Create<PlanForm>(http, auth);
        var chart = Create<PriceChart>(http, auth);

        await Initialize(dashboard);
        await Initialize(form);
        await Initialize(chart);

        Assert.Equal($"Nordpool {expected}", Field<string>(dashboard, "area"));
        Assert.Equal(expected, Field<string>(form, "area"));
        Assert.Null(Field<string?>(form, "loadError"));
        Assert.Null(Field<string?>(chart, "error"));
        Assert.Contains(requests, uri => uri.AbsolutePath.EndsWith("dashboard-showcase") && uri.Query == $"?areaCode={expected}");
    }

    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static T Create<T>(HttpClient http, AuthenticationStateProvider auth) where T : ComponentBase, new()
    {
        var component = new T();
        typeof(T).GetProperty("Http", Flags)!.SetValue(component, http);
        typeof(T).GetProperty("AuthProvider", Flags)!.SetValue(component, auth);
        typeof(T).GetProperty("Logger", Flags)!.SetValue(component, NullLogger<T>.Instance);
        return component;
    }

    private static Task Initialize(ComponentBase component) =>
        (Task)component.GetType().GetMethod("OnInitializedAsync", Flags)!.Invoke(component, null)!;

    private static T Field<T>(object component, string name) =>
        (T)component.GetType().GetField(name, Flags)!.GetValue(component)!;

    private sealed class StubHandler(List<Uri> requests) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TestAuthenticationStateProvider(string? area) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user")], "test");
            if (area is not null) identity.AddClaim(new Claim(CustomClaimTypes.PriceArea, area));
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }
}
