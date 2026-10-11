using BranikBot.Configuration;
using BranikBot.Persistence;
using BranikBot.Pricing;
using BranikBot.Pricing.Abstractions;
using BranikBot.Tests.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BranikBot.Tests.Pricing;

public sealed class PriceSamplingBackgroundServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _database = new();
    private readonly PriceHistoryStore _store;
    private readonly Mock<IMarketPriceProvider> _providerMock = new();
    private readonly Mock<IPriceSampleListener> _listenerMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly PriceSamplingBackgroundService _service;

    public PriceSamplingBackgroundServiceTests()
    {
        _store = new PriceHistoryStore(_database.Create());
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Now);

        var options = Options.Create(new MarketConfiguration
        {
            Url = "http://example.com/branik",
            SampleInterval = TimeSpan.FromHours(1)
        });

        _service = new PriceSamplingBackgroundService(_providerMock.Object, _store, _listenerMock.Object,
            _timeProviderMock.Object, options, Mock.Of<ILogger<PriceSamplingBackgroundService>>());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SampleAsync_ProviderReturnsPrice_StoresPriceWithCurrentTime()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ReturnsAsync(39.90m);

        // Act
        await _service.SampleAsync();

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
        await _service.SampleAsync();

        // Assert
        Assert.Equal(new PricePeriod(39.90m, earlier, earlier), _store.GetLatest());
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsNoPriceOnEmptyDatabase_StoresNothing()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ReturnsAsync((decimal?)null);

        // Act
        await _service.SampleAsync();

        // Assert
        Assert.Null(_store.GetLatest());
    }

    [Fact]
    public async Task SampleAsync_ProviderThrows_DoesNotThrow()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ThrowsAsync(new InvalidOperationException());

        // Act
        await _service.SampleAsync();

        // Assert
        Assert.Null(_store.GetLatest());
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsPrice_NotifiesListenerAfterStoring()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ReturnsAsync(39.90m);
        decimal? storedWhenNotified = null;
        _listenerMock.Setup(l => l.PriceSampledAsync())
            .Callback(() => storedWhenNotified = _store.GetLatest()?.Price)
            .Returns(Task.CompletedTask);

        // Act
        await _service.SampleAsync();

        // Assert
        _listenerMock.Verify(l => l.PriceSampledAsync(), Times.Once);
        Assert.Equal(39.90m, storedWhenNotified);
    }

    [Fact]
    public async Task SampleAsync_ProviderReturnsNoPrice_DoesNotNotifyListener()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ReturnsAsync((decimal?)null);

        // Act
        await _service.SampleAsync();

        // Assert
        _listenerMock.Verify(l => l.PriceSampledAsync(), Times.Never);
    }

    [Fact]
    public async Task SampleAsync_ProviderThrows_DoesNotNotifyListener()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ThrowsAsync(new InvalidOperationException());

        // Act
        await _service.SampleAsync();

        // Assert
        _listenerMock.Verify(l => l.PriceSampledAsync(), Times.Never);
    }

    [Fact]
    public async Task SampleAsync_ListenerThrows_DoesNotThrowAndKeepsStoredPrice()
    {
        // Arrange
        _providerMock.Setup(p => p.GetPriceAsync()).ReturnsAsync(39.90m);
        _listenerMock.Setup(l => l.PriceSampledAsync()).ThrowsAsync(new InvalidOperationException());

        // Act
        await _service.SampleAsync();

        // Assert
        Assert.Equal(new PricePeriod(39.90m, Now, Now), _store.GetLatest());
    }
}
