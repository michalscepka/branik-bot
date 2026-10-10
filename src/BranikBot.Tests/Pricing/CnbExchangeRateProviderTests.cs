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
    public async Task GetExchangeRatesAsync_FeedWithKnownAndUnknownCurrencies_ReturnsOnlyKnownOnes()
    {
        // Arrange
        SetupHttpResponse(CreateFeed(
            "<radek kod='AUD' mena='dolar' mnozstvi='1' kurz='13,955' zeme='Austrálie'/>",
            "<radek kod='EUR' mena='euro' mnozstvi='1' kurz='24,340' zeme='EMU'/>",
            "<radek kod='PLN' mena='zlotý' mnozstvi='1' kurz='5,742' zeme='Polsko'/>",
            "<radek kod='USD' mena='dolar' mnozstvi='1' kurz='20,871' zeme='USA'/>",
            "<radek kod='GBP' mena='libra' mnozstvi='1' kurz='28,012' zeme='Velká Británie'/>"));
        var provider = CreateProvider();

        // Act
        var rates = await provider.GetExchangeRatesAsync();

        // Assert
        Assert.NotNull(rates);
        Assert.Equal(4, rates.Count);
        Assert.Equal(24.340m, rates[Currency.Eur]);
        Assert.Equal(5.742m, rates[Currency.Pln]);
        Assert.Equal(20.871m, rates[Currency.Usd]);
        Assert.Equal(28.012m, rates[Currency.Gbp]);
    }

    [Fact]
    public async Task GetExchangeRatesAsync_RateQuotedForMoreUnits_ReturnsRateForOneUnit()
    {
        // Arrange
        SetupHttpResponse(CreateFeed("<radek kod='EUR' mena='euro' mnozstvi='10' kurz='250,000' zeme='EMU'/>"));
        var provider = CreateProvider();

        // Act
        var rates = await provider.GetExchangeRatesAsync();

        // Assert
        Assert.NotNull(rates);
        Assert.Equal(25.000m, rates[Currency.Eur]);
    }

    [Theory]
    [InlineData("<radek kod='EUR' mnozstvi='1' kurz='abc'/>")] // Rate is not a number
    [InlineData("<radek kod='EUR' mnozstvi='0' kurz='24,340'/>")] // Would divide by zero
    [InlineData("<radek kod='EUR' kurz='24,340'/>")] // No unit count
    [InlineData("<radek kod='EUR' mnozstvi='1'/>")] // No rate
    [InlineData("<radek mnozstvi='1' kurz='24,340'/>")] // No currency code
    [InlineData("<radek kod='CZK' mnozstvi='1' kurz='1,000'/>")] // The home currency has no rate
    [InlineData("<radek kod='1' mnozstvi='1' kurz='24,340'/>")] // An enum number is not a currency code
    public async Task GetExchangeRatesAsync_UnusableRow_SkipsIt(string row)
    {
        // Arrange
        SetupHttpResponse(CreateFeed(row, "<radek kod='USD' mnozstvi='1' kurz='20,871'/>"));
        var provider = CreateProvider();

        // Act
        var rates = await provider.GetExchangeRatesAsync();

        // Assert
        Assert.NotNull(rates);
        var rate = Assert.Single(rates);
        Assert.Equal(Currency.Usd, rate.Key);
    }

    [Fact]
    public async Task GetExchangeRatesAsync_FeedWithoutRows_ReturnsEmpty()
    {
        // Arrange
        SetupHttpResponse(CreateFeed());
        var provider = CreateProvider();

        // Act
        var rates = await provider.GetExchangeRatesAsync();

        // Assert
        Assert.NotNull(rates);
        Assert.Empty(rates);
    }

    [Fact]
    public async Task GetExchangeRatesAsync_RequestFails_ReturnsNull()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException());
        var provider = CreateProvider();

        // Act
        var rates = await provider.GetExchangeRatesAsync();

        // Assert
        Assert.Null(rates);
    }

    private CnbExchangeRateProvider CreateProvider() => new(_httpClient, _options, _loggerMock.Object);

    private static string CreateFeed(params string[] rows)
    {
        return $"""
                <?xml version='1.0' encoding='UTF-8'?>
                <kurzy banka='CNB' datum='09.01.2026' poradi='6'>
                <tabulka typ='XML_TYP_CNB_KURZY_DEVIZOVEHO_TRHU'>
                {string.Join(Environment.NewLine, rows)}
                </tabulka>
                </kurzy>
                """;
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
