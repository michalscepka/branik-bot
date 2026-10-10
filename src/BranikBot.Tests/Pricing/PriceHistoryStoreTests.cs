using BranikBot.Pricing;

namespace BranikBot.Tests.Pricing;

public sealed class PriceHistoryStoreTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _database = new();
    private readonly PriceHistoryStore _store;

    public PriceHistoryStoreTests()
    {
        _store = new PriceHistoryStore(_database.Create());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void GetLatest_EmptyDatabase_ReturnsNull()
    {
        // Act
        var latest = _store.GetLatest();

        // Assert
        Assert.Null(latest);
    }

    [Fact]
    public void Record_FirstPrice_StoresPeriodSeenOnce()
    {
        // Act
        _store.Record(39.90m, Start);

        // Assert
        Assert.Equal(new PricePeriod(39.90m, Start, Start), _store.GetLatest());
    }

    [Fact]
    public void Record_SamePriceAgain_ExtendsLastSeenOnly()
    {
        // Arrange
        _store.Record(39.90m, Start);

        // Act
        _store.Record(39.90m, Start.AddHours(1));

        // Assert
        Assert.Equal(new PricePeriod(39.90m, Start, Start.AddHours(1)), _store.GetLatest());
    }

    [Fact]
    public void Record_ChangedPrice_StartsNewPeriod()
    {
        // Arrange
        _store.Record(39.90m, Start);

        // Act
        _store.Record(44.90m, Start.AddHours(1));

        // Assert
        Assert.Equal(new PricePeriod(44.90m, Start.AddHours(1), Start.AddHours(1)), _store.GetLatest());
    }

    [Fact]
    public void Record_PriceReturnsToEarlierValue_StartsNewPeriod()
    {
        // Arrange
        _store.Record(39.90m, Start);
        _store.Record(44.90m, Start.AddHours(1));

        // Act
        _store.Record(39.90m, Start.AddHours(2));

        // Assert
        Assert.Equal(new PricePeriod(39.90m, Start.AddHours(2), Start.AddHours(2)), _store.GetLatest());
    }

    [Fact]
    public void GetLatest_NewStoreOnSameFile_ReturnsStoredPrice()
    {
        // Arrange
        _store.Record(39.90m, Start);

        // Act
        var latest = new PriceHistoryStore(_database.Create()).GetLatest();

        // Assert
        Assert.Equal(new PricePeriod(39.90m, Start, Start), latest);
    }
}
