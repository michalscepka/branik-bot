using BranikBot.Persistence;
using BranikBot.Pricing;
using BranikBot.Replies;
using BranikBot.Tests.Persistence;
using Microsoft.Extensions.Logging;
using Moq;

namespace BranikBot.Tests.Replies;

public sealed class ReplyBuilderTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _database = new();
    private readonly PriceHistoryStore _priceHistoryStore;
    private readonly LatestExchangeRate _latestExchangeRate = new();
    private readonly ReplyBuilder _builder;

    public ReplyBuilderTests()
    {
        _priceHistoryStore = new PriceHistoryStore(_database.Create());
        _builder = new ReplyBuilder(_priceHistoryStore, _latestExchangeRate, Mock.Of<ILogger<ReplyBuilder>>());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void Build_TextWithoutAmounts_ReturnsNull()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);

        // Act
        var reply = _builder.Build("ahoj, jak se mas?");

        // Assert
        Assert.Null(reply);
    }

    [Fact]
    public void Build_NoPriceStored_ReturnsNull()
    {
        // Act
        var reply = _builder.Build("mam 250 kc");

        // Assert
        Assert.Null(reply);
    }

    [Fact]
    public void Build_CzkAmountWithStoredPrice_ReturnsReply()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);

        // Act
        var reply = _builder.Build("mam 250 kc");

        // Assert
        Assert.NotNull(reply);
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", reply);
    }

    [Fact]
    public void Build_EurAmountWithExchangeRate_ReturnsReply()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);
        _latestExchangeRate.Set(Currency.Eur, 25m);

        // Act
        var reply = _builder.Build("mam 10 eur");

        // Assert
        Assert.NotNull(reply);
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", reply);
    }

    [Fact]
    public void Build_EurAmountWithoutExchangeRate_ReturnsNull()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);

        // Act
        var reply = _builder.Build("mam 10 eur");

        // Assert
        Assert.Null(reply);
    }

    [Fact]
    public void Build_MixedAmountsWithoutExchangeRate_ReturnsOnlyCzkLines()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);

        // Act
        var reply = _builder.Build("mam 250 kc a 10 eur");

        // Assert
        Assert.NotNull(reply);
        Assert.Contains("> 250 kc", reply);
        Assert.DoesNotContain("10 eur", reply);
    }
}
