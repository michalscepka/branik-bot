using System.Net;
using BranikBot.Configuration;
using BranikBot.Pricing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace BranikBot.Tests.Pricing;

public class AkcniCenyProviderTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly AkcniCenyProvider _provider;

    public AkcniCenyProviderTests()
    {
        var options = Options.Create(new MarketConfiguration
        {
            Url = "http://example.com/branik",
            SampleInterval = TimeSpan.FromHours(1)
        });

        _provider = new AkcniCenyProvider(new HttpClient(_httpMessageHandlerMock.Object),
            Mock.Of<ILogger<AkcniCenyProvider>>(), options);
    }

    [Fact]
    public async Task GetPriceAsync_PageContainsLowPrice_ReturnsPrice()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "<html><body><span itemprop='lowPrice' content='39.90'>39,90</span></body></html>");

        // Act
        var price = await _provider.GetPriceAsync();

        // Assert
        Assert.Equal(39.90m, price);
    }

    [Theory]
    [InlineData("<html><body>Nic tu neni</body></html>")]
    [InlineData("<html><body><span itemprop='lowPrice'>39,90</span></body></html>")]
    [InlineData("<html><body><span itemprop='lowPrice' content='0'>0</span></body></html>")]
    public async Task GetPriceAsync_PageWithoutUsablePrice_ReturnsNull(string html)
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, html);

        // Act
        var price = await _provider.GetPriceAsync();

        // Assert
        Assert.Null(price);
    }

    [Fact]
    public async Task GetPriceAsync_RequestFails_ReturnsNull()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, string.Empty);

        // Act
        var price = await _provider.GetPriceAsync();

        // Assert
        Assert.Null(price);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }
}
