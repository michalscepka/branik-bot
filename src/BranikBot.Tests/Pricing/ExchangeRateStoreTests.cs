using BranikBot.Pricing;

namespace BranikBot.Tests.Pricing;

public sealed class ExchangeRateStoreTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _database = new();
    private readonly ExchangeRateStore _store;

    public ExchangeRateStoreTests()
    {
        _store = new ExchangeRateStore(_database.Create());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void GetLatest_EmptyDatabase_ReturnsNull()
    {
        // Act
        var rate = _store.GetLatest(Currency.Eur);

        // Assert
        Assert.Null(rate);
    }

    [Fact]
    public void Record_FirstRate_StoresRate()
    {
        // Act
        _store.Record(Currency.Eur, 24.340m, Now);

        // Assert
        Assert.Equal(24.340m, _store.GetLatest(Currency.Eur));
    }

    [Fact]
    public void Record_NewRate_ReplacesPreviousRate()
    {
        // Arrange
        _store.Record(Currency.Eur, 24.340m, Now);

        // Act
        _store.Record(Currency.Eur, 25.105m, Now.AddHours(6));

        // Assert
        Assert.Equal(25.105m, _store.GetLatest(Currency.Eur));
    }

    [Fact]
    public void GetLatest_ReopenedDatabase_ReturnsStoredRate()
    {
        // Arrange
        _store.Record(Currency.Eur, 24.340m, Now);

        // Act
        var rate = new ExchangeRateStore(_database.Create()).GetLatest(Currency.Eur);

        // Assert
        Assert.Equal(24.340m, rate);
    }
}
