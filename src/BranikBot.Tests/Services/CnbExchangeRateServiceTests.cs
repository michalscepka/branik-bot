using System.Net;
using BranikBot.Infrastructure.Configuration;
using BranikBot.Infrastructure.Enums;
using BranikBot.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace BranikBot.Tests.Services;

public class CnbExchangeRateServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<CnbExchangeRateService>> _loggerMock;
    private readonly IOptions<ExchangeRateConfiguration> _options;

    public CnbExchangeRateServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<CnbExchangeRateService>>();
        _options = Options.Create(new ExchangeRateConfiguration
        {
            Url = "http://example.com/rates.xml",
            CacheDuration = TimeSpan.FromMinutes(10)
        });

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_httpMessageHandlerMock.Object));
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

        var service = new CnbExchangeRateService(_httpClientFactoryMock.Object, _memoryCache, _options, _loggerMock.Object);

        // Act
        var rate = await service.GetExchangeRateAsync(Currency.Eur);

        // Assert
        Assert.Equal(24.340m, rate);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ReturnsCorrectRate_ForCurrencyWithAmountNotOne()
    {
        // This test simulates a currency like JPY which has mnozstvi="100".
        // Though our Enum currently only has Czk and Eur.
        // Let's assume we might add JPY later or verify logic handles logic.
        // But Enum.TryParse will fail for JPY if not in Enum.
        // So we can only test EUR now unless we add value to Enum temporarily or mock it.
        // However, the service logic uses Enum.TryParse(code).
        // So we can only test what is in Currency enum.

        // Let's assume EUR might have amount != 1 (unlikely but logic should handle it).

        var xmlContent = @"<?xml version='1.0' encoding='UTF-8'?>
<kurzy banka='CNB' datum='09.01.2026' poradi='6'>
<tabulka typ='XML_TYP_CNB_KURZY_DEVIZOVEHO_TRHU'>
<radek kod='EUR' mena='euro' mnozstvi='10' kurz='250,000' zeme='EMU'/>
</tabulka>
</kurzy>";

        SetupHttpResponse(xmlContent);
        var service = new CnbExchangeRateService(_httpClientFactoryMock.Object, _memoryCache, _options, _loggerMock.Object);

        // Act
        var rate = await service.GetExchangeRateAsync(Currency.Eur);

        // Assert
        Assert.Equal(25.000m, rate);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ReturnsOne_ForCzk()
    {
        var service = new CnbExchangeRateService(_httpClientFactoryMock.Object, _memoryCache, _options, _loggerMock.Object);
        var rate = await service.GetExchangeRateAsync(Currency.Czk);
        Assert.Equal(1m, rate);
    }

    [Fact]
    public async Task GetExchangeRateAsync_CachesResult()
    {
        // Arrange
        var xmlContent = @"<?xml version='1.0' encoding='UTF-8'?>
<kurzy banka='CNB' datum='09.01.2026' poradi='6'>
<tabulka typ='XML_TYP_CNB_KURZY_DEVIZOVEHO_TRHU'>
<radek kod='EUR' mena='euro' mnozstvi='1' kurz='24,340' zeme='EMU'/>
</tabulka>
</kurzy>";

        SetupHttpResponse(xmlContent);
        var service = new CnbExchangeRateService(_httpClientFactoryMock.Object, _memoryCache, _options, _loggerMock.Object);

        // Act
        await service.GetExchangeRateAsync(Currency.Eur);
        await service.GetExchangeRateAsync(Currency.Eur); // Second call

        // Assert
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
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
