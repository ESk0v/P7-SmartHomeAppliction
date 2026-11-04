using System.Net;
using Moq;
using Moq.Protected;

namespace SmartHomeApplication.Tests.Helpers;

public static class HttpClientTestHelper
{
    public static (HttpClient client, Mock<HttpMessageHandler> handlerMock) CreateMockHttpClient(
        HttpStatusCode statusCode, 
        string responseContent = "{}")
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:7000/")
        };

        return (client, handlerMock);
    }
}