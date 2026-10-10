using BranikBot.Configuration;
using BranikBot.Pricing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BranikBot.Tests.Pricing;

public sealed class ExchangeRateSamplerTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _database = new();
    private readonly ExchangeRateStore _store;
    private readonly Mock<IExchangeRateProvider> _providerMock = new();
    private readonly ExchangeRateSampler _sampler;

    public ExchangeRateSamplerTests()
    {
        _store = new ExchangeRateStore(_database.Create());

        var timeProviderMock = new Mock<TimeProvider>();
        timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Now);

        var options = Options.Create(new ExchangeRateConfiguration
        {
            Url = "http://example.com/rates.xml",
            SampleInterval = TimeSpan.FromHours(1)
        });

        _sampler = new ExchangeRateSampler(_providerMock.Object, _store, timeProviderMock.Object, options,
            Mock.Of<ILogger<ExchangeRateSampler>>());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SampleAsync_ProviderReturnsRate_StoresRate()
    {
        // Arrange
        _providerMock.Setup(p => p.GetExchangeRateAsync(Currency.Eur)).ReturnsAsync(24.340m);

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Equal(24.340m, _store.GetLatest(Currency.Eur));
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsZero_KeepsLastStoredRate()
    {
        // Arrange
        _store.Record(Currency.Eur, 24.340m, Now.AddHours(-1));
        _providerMock.Setup(p => p.GetExchangeRateAsync(Currency.Eur)).ReturnsAsync(0m);

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Equal(24.340m, _store.GetLatest(Currency.Eur));
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsZeroOnEmptyDatabase_StoresNothing()
    {
        // Arrange
        _providerMock.Setup(p => p.GetExchangeRateAsync(Currency.Eur)).ReturnsAsync(0m);

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Null(_store.GetLatest(Currency.Eur));
    }

    [Fact]
    public async Task SampleAsync_ProviderThrows_DoesNotThrow()
    {
        // Arrange
        _providerMock.Setup(p => p.GetExchangeRateAsync(Currency.Eur)).ThrowsAsync(new InvalidOperationException());

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Null(_store.GetLatest(Currency.Eur));
    }
}
