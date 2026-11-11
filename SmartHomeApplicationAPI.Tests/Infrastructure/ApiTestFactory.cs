using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SmartHomeApplicationAPI.Service;

namespace SmartHomeApplicationAPI.Tests.Infrastructure;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SmartHome", 
        "Host=localhost;Port=5433;Database=SmartHomeTestDB;Username=testuser;Password=testpassword");
        builder.UseSetting("Hangfire:RunServer", "false");

        builder.ConfigureTestServices(services =>
        {
           services.AddHttpClient<IForecastingService, ForecastingService>()
           .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler());
        });
    }
}


internal sealed class MockHttpMessageHandler() : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                    "days": [
                        {
                            "date": "2026-11-12",
                            "prices": [
                                {"hour": 10, "price": 1.25}
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


// Factory that should: (NOTES FOR MYSELF AFTER READING MICROSOFT GUIDE)
    // use postgres connection string: Host=localhost;Port=5433;Database=SmartHomeTestDB;Username=testuser;Password=testpassword
    // replace appllication connection string
    // set Hangfire:RunServer to fals
    // replace the forecasting HttpClient with a fake HttpMessageHandler
