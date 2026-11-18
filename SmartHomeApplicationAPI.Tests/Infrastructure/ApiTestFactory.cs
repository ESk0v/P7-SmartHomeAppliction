using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SmartHomeApplicationAPI.Hangfire.Services;

namespace SmartHomeApplicationAPI.Tests.Infrastructure;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Hangfire:RunServer", "false");
        
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<IForecastingService, ForecastingService>()
                .ConfigurePrimaryHttpMessageHandler(
                    () => new MockHttpMessageHandler());
        });
    }
}

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Query.Contains("area=EMP") == true)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"days": []}""",
                    Encoding.UTF8,
                    "application/json")
            });
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""
                {
                    "days": [
                        {
                            "date": "{{DateTime.UtcNow.Date.AddDays(2):yyyy-MM-dd}}",
                            "prices": [
                                {
                                    "hour": 10,
                                    "price": 1.25
                                }
                            ]
                        }
                    ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        };

        return Task.FromResult(response);
    }
}
