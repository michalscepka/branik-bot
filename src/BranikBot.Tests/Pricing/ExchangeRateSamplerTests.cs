using BranikBot.Configuration;
using BranikBot.Pricing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BranikBot.Tests.Pricing;

public class ExchangeRateSamplerTests
{
    private readonly LatestExchangeRate _latestExchangeRate = new();
    private readonly Mock<IExchangeRateProvider> _providerMock = new();
    private readonly ExchangeRateSampler _sampler;

    public ExchangeRateSamplerTests()
    {
        var options = Options.Create(new ExchangeRateConfiguration
        {
            Url = "http://example.com/rates.xml",
            SampleInterval = TimeSpan.FromHours(1)
        });

        _sampler = new ExchangeRateSampler(_providerMock.Object, _latestExchangeRate, TimeProvider.System, options,
            Mock.Of<ILogger<ExchangeRateSampler>>());
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsRate_SetsLatestRate()
    {
        // Arrange
        _providerMock.Setup(p => p.GetExchangeRateAsync(Currency.Eur)).ReturnsAsync(24.340m);

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Equal(24.340m, _latestExchangeRate.Get(Currency.Eur));
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsNoRate_KeepsLastRate()
    {
        // Arrange
        _latestExchangeRate.Set(Currency.Eur, 24.340m);
        _providerMock.Setup(p => p.GetExchangeRateAsync(Currency.Eur)).ReturnsAsync((decimal?)null);

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Equal(24.340m, _latestExchangeRate.Get(Currency.Eur));
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsNoRateWithNoRate_SetsNothing()
    {
        // Arrange
        _providerMock.Setup(p => p.GetExchangeRateAsync(Currency.Eur)).ReturnsAsync((decimal?)null);

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Null(_latestExchangeRate.Get(Currency.Eur));
    }

    [Fact]
    public async Task SampleAsync_ProviderThrows_DoesNotThrow()
    {
        // Arrange
        _providerMock.Setup(p => p.GetExchangeRateAsync(Currency.Eur)).ThrowsAsync(new InvalidOperationException());

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Null(_latestExchangeRate.Get(Currency.Eur));
    }
}
