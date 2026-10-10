using BranikBot.Configuration;
using BranikBot.Pricing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BranikBot.Tests.Pricing;

public sealed class PriceSamplerTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _database = new();
    private readonly PriceHistoryStore _store;
    private readonly Mock<IMarketPriceProvider> _providerMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly PriceSampler _sampler;

    public PriceSamplerTests()
    {
        _store = new PriceHistoryStore(_database.Create());
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Now);

        var options = Options.Create(new MarketConfiguration
        {
            Url = "http://example.com/branik",
            SampleInterval = TimeSpan.FromHours(1)
        });

        _sampler = new PriceSampler(_providerMock.Object, _store, _timeProviderMock.Object, options,
            Mock.Of<ILogger<PriceSampler>>());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SampleAsync_ProviderReturnsPrice_StoresPriceWithCurrentTime()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ReturnsAsync(39.90m);

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Equal(new PricePeriod(39.90m, Now, Now), _store.GetLatest());
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsNoPrice_KeepsLastStoredPrice()
    {
        // Arrange
        var earlier = Now.AddHours(-1);
        _store.Record(39.90m, earlier);
        _providerMock.Setup(p => p.GetPriceAsync()).ReturnsAsync((decimal?)null);

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Equal(new PricePeriod(39.90m, earlier, earlier), _store.GetLatest());
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsNoPriceOnEmptyDatabase_StoresNothing()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ReturnsAsync((decimal?)null);

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Null(_store.GetLatest());
    }

    [Fact]
    public async Task SampleAsync_ProviderThrows_DoesNotThrow()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ThrowsAsync(new InvalidOperationException());

        // Act
        await _sampler.SampleAsync();

        // Assert
        Assert.Null(_store.GetLatest());
    }
}
