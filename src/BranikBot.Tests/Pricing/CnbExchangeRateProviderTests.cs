using System.Net;
using BranikBot.Configuration;
using BranikBot.Pricing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace BranikBot.Tests.Pricing;

public class CnbExchangeRateProviderTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<CnbExchangeRateProvider>> _loggerMock;
    private readonly IOptions<ExchangeRateConfiguration> _options;

    public CnbExchangeRateProviderTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _loggerMock = new Mock<ILogger<CnbExchangeRateProvider>>();
        _options = Options.Create(new ExchangeRateConfiguration
        {
            Url = "http://example.com/rates.xml",
            SampleInterval = TimeSpan.FromMinutes(10)
        });
    }

    [Fact]
    public async Task GetExchangeRateAsync_ReturnsCorrectRate_ForEur()
    {
        // Arrange
        var xmlContent = @"<?xml version='1.0' encoding='UTF-8'?>
<kurzy banka='CNB' datum='09.01.2026' poradi='6'>
<tabulka typ='XML_TYP_CNB_KURZY_DEVIZOVEHO_TRHU'>
<radek kod='AUD' mena='dolar' mnozstvi='1' kurz='13,955' zeme='Austrálie'/>
<radek kod='EUR' mena='euro' mnozstvi='1' kurz='24,340' zeme='EMU'/>
</tabulka>
</kurzy>";

        SetupHttpResponse(xmlContent);

        var service = new CnbExchangeRateProvider(_httpClient, _options, _loggerMock.Object);

        // Act
        var rate = await service.GetExchangeRateAsync(Currency.Eur);

        // Assert
        Assert.Equal(24.340m, rate);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ReturnsCorrectRate_ForCurrencyWithAmountNotOne()
    {
        var xmlContent = @"<?xml version='1.0' encoding='UTF-8'?>
<kurzy banka='CNB' datum='09.01.2026' poradi='6'>
<tabulka typ='XML_TYP_CNB_KURZY_DEVIZOVEHO_TRHU'>
<radek kod='EUR' mena='euro' mnozstvi='10' kurz='250,000' zeme='EMU'/>
</tabulka>
</kurzy>";

        SetupHttpResponse(xmlContent);
        var service = new CnbExchangeRateProvider(_httpClient, _options, _loggerMock.Object);

        // Act
        var rate = await service.GetExchangeRateAsync(Currency.Eur);

        // Assert
        Assert.Equal(25.000m, rate);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ReturnsOne_ForCzk()
    {
        var service = new CnbExchangeRateProvider(_httpClient, _options, _loggerMock.Object);
        var rate = await service.GetExchangeRateAsync(Currency.Czk);
        Assert.Equal(1m, rate);
    }

    [Fact]
    public async Task GetExchangeRateAsync_RequestFails_ReturnsZero()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException());

        var service = new CnbExchangeRateProvider(_httpClient, _options, _loggerMock.Object);

        // Act
        var rate = await service.GetExchangeRateAsync(Currency.Eur);

        // Assert
        Assert.Equal(0m, rate);
    }

    private void SetupHttpResponse(string content)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });
    }
}
