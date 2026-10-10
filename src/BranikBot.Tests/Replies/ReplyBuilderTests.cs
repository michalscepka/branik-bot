using BranikBot.Configuration;
using BranikBot.Persistence;
using BranikBot.Pricing;
using BranikBot.Replies;
using BranikBot.Tests.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        _builder = CreateBuilder(maxAmounts: 10);
    }

    public void Dispose() => _database.Dispose();

    private ReplyBuilder CreateBuilder(int maxAmounts)
    {
        var configuration = new DiscordConfiguration { MaxAmountsPerReply = maxAmounts };
        return new ReplyBuilder(_priceHistoryStore, _latestExchangeRate, Options.Create(configuration), FixedPickRandom.First,
            Mock.Of<ILogger<ReplyBuilder>>());
    }

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

    [Fact]
    public void Build_MoreAmountsThanConfiguredMaximum_AnswersOnlyTheLargest()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);
        var builder = CreateBuilder(maxAmounts: 1);

        // Act
        var reply = builder.Build("mam 250 kc, 900 kc a 400 kc");

        // Assert
        Assert.NotNull(reply);
        Assert.Contains("> 900 kc", reply);
        Assert.DoesNotContain("250 kc", reply);
        Assert.DoesNotContain("400 kc", reply);
    }

    [Fact]
    public void Build_TextStartingWithUserMention_IgnoresTheIdInTheMention()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);
        var builder = CreateBuilder(maxAmounts: 1);

        // Act
        var reply = builder.Build("<@1200554094708924426> 500 kc");

        // Assert
        Assert.NotNull(reply);
        Assert.StartsWith("> 500 kc", reply);
        Assert.Contains("To by stačilo na 5 dvoulitrovek Braníčka ve slevě!", reply);
    }

    [Fact]
    public void Build_TextWithOnlyUserMention_ReturnsNull()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);

        // Act
        var reply = _builder.Build("<@1200554094708924426>");

        // Assert
        Assert.Null(reply);
    }

    [Theory]
    [InlineData("mam 10 usd", Currency.Usd, "za jeden dolar.")]
    [InlineData("mam $10", Currency.Usd, "za jeden dolar.")]
    [InlineData("mam 10 liber", Currency.Gbp, "za jednu libru.")]
    [InlineData("mam 10 zł", Currency.Pln, "za jeden zlotý.")]
    public void Build_ForeignAmountWithExchangeRate_ReturnsReplyNamingTheCurrency(
        string content, Currency currency, string expectedEnding)
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);
        _latestExchangeRate.Set(currency, 25m);

        // Act
        var reply = _builder.Build(content);

        // Assert
        Assert.NotNull(reply);
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", reply);
        Assert.Contains(expectedEnding, reply);
    }

    [Fact]
    public void Build_ForeignAmountWithRateOfAnotherCurrencyOnly_ReturnsNull()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);
        _latestExchangeRate.Set(Currency.Eur, 25m);

        // Act
        var reply = _builder.Build("mam 10 usd");

        // Assert
        Assert.Null(reply);
    }

    [Fact]
    public void Build_TwoForeignAmountsWithOneRate_AnswersOnlyTheOneWithRate()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);
        _latestExchangeRate.Set(Currency.Usd, 20m);

        // Act
        var reply = _builder.Build("mam 1000 liber a 10 usd");

        // Assert
        Assert.NotNull(reply);
        Assert.Contains("> 10 usd", reply);
        Assert.DoesNotContain("1000 liber", reply);
    }

    [Fact]
    public void Build_WithRandomPickingSecondPhrasing_ReturnsThatPhrasing()
    {
        // Arrange
        _priceHistoryStore.Record(100m, Now);
        var configuration = new DiscordConfiguration { MaxAmountsPerReply = 1 };
        var builder = new ReplyBuilder(_priceHistoryStore, _latestExchangeRate, Options.Create(configuration),
            new FixedPickRandom(1), Mock.Of<ILogger<ReplyBuilder>>());

        // Act
        var reply = builder.Build("mam 250 kc");

        // Assert
        Assert.NotNull(reply);
        Assert.Contains("Za to máš 2 dvoulitrovky Braníčka ve slevě!", reply);
    }
}
