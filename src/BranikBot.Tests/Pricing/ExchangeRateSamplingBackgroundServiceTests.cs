using BranikBot.Configuration;
using BranikBot.Pricing;
using BranikBot.Pricing.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BranikBot.Tests.Pricing;

public class ExchangeRateSamplingBackgroundServiceTests
{
    private readonly LatestExchangeRate _latestExchangeRate = new();
    private readonly Mock<IExchangeRateProvider> _providerMock = new();
    private readonly ExchangeRateSamplingBackgroundService _service;

    public ExchangeRateSamplingBackgroundServiceTests()
    {
        var options = Options.Create(new ExchangeRateConfiguration
        {
            Url = "http://example.com/rates.xml",
            SampleInterval = TimeSpan.FromHours(1)
        });

        _service = new ExchangeRateSamplingBackgroundService(_providerMock.Object, _latestExchangeRate, TimeProvider.System, options,
            Mock.Of<ILogger<ExchangeRateSamplingBackgroundService>>());
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsRates_SetsLatestRateOfEachCurrency()
    {
        // Arrange
        SetupRates(new() { [Currency.Eur] = 24.340m, [Currency.Usd] = 20.871m, [Currency.Pln] = 5.742m });

        // Act
        await _service.SampleAsync();

        // Assert
        Assert.Equal(24.340m, _latestExchangeRate.Get(Currency.Eur));
        Assert.Equal(20.871m, _latestExchangeRate.Get(Currency.Usd));
        Assert.Equal(5.742m, _latestExchangeRate.Get(Currency.Pln));
        Assert.Null(_latestExchangeRate.Get(Currency.Gbp));
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsNothing_KeepsLastRates()
    {
        // Arrange
        _latestExchangeRate.Set(Currency.Eur, 24.340m);
        SetupRates(null);

        // Act
        await _service.SampleAsync();

        // Assert
        Assert.Equal(24.340m, _latestExchangeRate.Get(Currency.Eur));
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsNothingWithNoRate_SetsNothing()
    {
        // Arrange
        SetupRates(null);

        // Act
        await _service.SampleAsync();

        // Assert
        Assert.Null(_latestExchangeRate.Get(Currency.Eur));
    }

    [Fact]
    public async Task SampleAsync_CurrencyMissingFromNewRates_KeepsItsLastRate()
    {
        // Arrange
        _latestExchangeRate.Set(Currency.Usd, 20.871m);
        SetupRates(new() { [Currency.Eur] = 24.340m });

        // Act
        await _service.SampleAsync();

        // Assert
        Assert.Equal(24.340m, _latestExchangeRate.Get(Currency.Eur));
        Assert.Equal(20.871m, _latestExchangeRate.Get(Currency.Usd));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SampleAsync_RateIsNotPositive_KeepsLastRate(decimal rate)
    {
        // Arrange
        _latestExchangeRate.Set(Currency.Eur, 24.340m);
        SetupRates(new() { [Currency.Eur] = rate, [Currency.Usd] = 20.871m });

        // Act
        await _service.SampleAsync();

        // Assert
        Assert.Equal(24.340m, _latestExchangeRate.Get(Currency.Eur));
        Assert.Equal(20.871m, _latestExchangeRate.Get(Currency.Usd));
    }

    [Fact]
    public async Task SampleAsync_ProviderThrows_DoesNotThrow()
    {
        // Arrange
        _providerMock.Setup(p => p.GetExchangeRatesAsync()).ThrowsAsync(new InvalidOperationException());

        // Act
        await _service.SampleAsync();

        // Assert
        Assert.Null(_latestExchangeRate.Get(Currency.Eur));
    }

    private void SetupRates(Dictionary<Currency, decimal>? rates)
    {
        _providerMock.Setup(p => p.GetExchangeRatesAsync()).ReturnsAsync(rates);
    }
}
