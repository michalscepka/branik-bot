using BranikBot.Parsing;
using BranikBot.Pricing;
using BranikBot.Replies;

namespace BranikBot.Tests.Replies;

public class MessageFormatterTests
{
    private const int MaxAmounts = 10;

    private static readonly Dictionary<Currency, decimal> NoRates = [];

    private static Dictionary<Currency, decimal> Rate(Currency currency, decimal rate) => new() { [currency] = rate };

    [Fact]
    public void FormatMessage_WithEmptyInputs_ReturnsEmptyString()
    {
        // Act
        var result = MessageFormatter.FormatMessage([], 100m, NoRates, MaxAmounts);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FormatMessage_WithCzkInput_InsufficientFunds_ReturnsInsufficientFundsMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(50m, Currency.Czk, "50 kc") };
        var marketPrice = 100m;

        // Act
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, NoRates, MaxAmounts);

        // Assert
        Assert.Contains("Lítost má v srdci, ale 50 kc nepostačí ani na jednu číši Braníčka plastového ve slevě.", result);
    }

    [Fact]
    public void FormatMessage_WithCzkInput_SufficientFunds_LessThen100Bottles_ReturnsBottleMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(250m, Currency.Czk, "250 kc") };
        var marketPrice = 100m; // 2 bottles

        // Act
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, NoRates, MaxAmounts);

        // Assert
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", result);
    }

    [Fact]
    public void FormatMessage_WithCzkInput_SufficientFunds_Between100And999Bottles_ReturnsParcelMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(12000m, Currency.Czk, "12000 kc") };
        var marketPrice = 100m; // 120 bottles = 20 parcels

        // Act
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, NoRates, MaxAmounts);

        // Assert
        Assert.Contains("To by stačilo na 20 baliku (120 dvoulitrovek) Braníčka ve slevě!", result);
    }

    [Fact]
    public void FormatMessage_WithCzkInput_SufficientFunds_Between1000And100000Bottles_ReturnsPalletMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(120000m, Currency.Czk, "120000 kc") };
        var marketPrice = 100m; // 1200 bottles = 200 parcels = 2 pallets

        // Act
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, NoRates, MaxAmounts);

        // Assert
        Assert.Contains("To by stačilo na 2 europalety (200 baliku) dvoulitrovek Braníčka ve slevě!", result);
    }

    [Fact]
    public void FormatMessage_WithCzkInput_TooRich_ReturnsTooRichMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(20_000_000m, Currency.Czk, "20M kc") };
        var marketPrice = 100m; // 200,000 bottles > 100,000

        // Act
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, NoRates, MaxAmounts);

        // Assert
        Assert.Contains("Za tolik by sis mohl koupit rovnou celý Plzeňský Prazdroj!", result);
    }

    [Theory]
    [InlineData(100_000_000_000)] // 100 mld, more bottles than fit into an int
    [InlineData(1_000_000_000_000_000_000)] // The largest amount the parser produces
    public void FormatMessage_WithMoreBottlesThanIntCanHold_ReturnsTooRichMessage(decimal amount)
    {
        // Arrange
        var inputs = new[] { new UserInput(amount, Currency.Eur, "hodně eur") };
        var marketPrice = 0.01m;
        var euroExchangeRate = 25m;

        // Act
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, Rate(Currency.Eur, euroExchangeRate), MaxAmounts);

        // Assert
        Assert.Contains("Za tolik by sis mohl koupit rovnou celý Plzeňský Prazdroj!", result);
    }

    [Fact]
    public void FormatMessage_WithEurInput_ReturnsMessageWithExchangeRateNote()
    {
        // Arrange
        var inputs = new[] { new UserInput(10m, Currency.Eur, "10 eur") };
        var marketPrice = 100m;
        var exchangeRate = 25m; // 10 EUR * 25 = 250 CZK = 2 bottles

        // Act
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, Rate(Currency.Eur, exchangeRate), MaxAmounts);

        // Assert
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", result);
        Assert.Contains("Při aktuálním směnném kurzu ve výši", result);
        Assert.Contains("25", result);
        Assert.Contains("za jedno euro.", result);
    }

    [Fact]
    public void FormatMessage_WithMultipleInputs_ReturnsAllLines()
    {
        // Arrange
        var inputs = new[]
        {
            new UserInput(250m, Currency.Czk, "250 kc"),
            new UserInput(10m, Currency.Eur, "10 eur")
        };
        var marketPrice = 100m;
        var exchangeRate = 25m;

        // Act
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, Rate(Currency.Eur, exchangeRate), MaxAmounts);

        // Assert
        Assert.Contains("> 250 kc", result);
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", result);
        Assert.Contains("> 10 eur", result);
        Assert.Contains("Při aktuálním směnném kurzu ve výši", result);
    }

    [Fact]
    public void FormatMessage_WithEurInputAndNoExchangeRate_ReturnsEmptyString()
    {
        // Arrange
        var inputs = new[] { new UserInput(10m, Currency.Eur, "10 eur") };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, NoRates, MaxAmounts);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FormatMessage_WithMixedInputsAndNoExchangeRate_ReturnsOnlyCzkLines()
    {
        // Arrange
        var inputs = new[]
        {
            new UserInput(250m, Currency.Czk, "250 kc"),
            new UserInput(10m, Currency.Eur, "10 eur")
        };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, NoRates, MaxAmounts);

        // Assert
        Assert.Contains("> 250 kc", result);
        Assert.DoesNotContain("10 eur", result);
    }

    [Fact]
    public void FormatMessage_WithMoreInputsThanMaximum_ReturnsOnlyTheLargest()
    {
        // Arrange
        var inputs = new[]
        {
            new UserInput(250m, Currency.Czk, "250 kc"),
            new UserInput(900m, Currency.Czk, "900 kc"),
            new UserInput(400m, Currency.Czk, "400 kc")
        };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, NoRates, maxAmounts: 2);

        // Assert
        Assert.Equal(
            """
            > 900 kc
            To by stačilo na 9 dvoulitrovek Braníčka ve slevě!

            > 400 kc
            To by stačilo na 4 dvoulitrovky Braníčka ve slevě!


            """.ReplaceLineEndings(), result);
    }

    [Fact]
    public void FormatMessage_WithEurLargerThanCzkAfterConversion_PicksTheEurAmount()
    {
        // Arrange
        var inputs = new[]
        {
            new UserInput(250m, Currency.Czk, "250 kc"),
            new UserInput(20m, Currency.Eur, "20 eur")
        };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, Rate(Currency.Eur, 25m), maxAmounts: 1);

        // Assert
        Assert.Contains("> 20 eur", result);
        Assert.DoesNotContain("250 kc", result);
    }

    [Fact]
    public void FormatMessage_WithLargestEurAndNoExchangeRate_FallsBackToCzkAmount()
    {
        // Arrange
        var inputs = new[]
        {
            new UserInput(250m, Currency.Czk, "250 kc"),
            new UserInput(1000m, Currency.Eur, "1000 eur")
        };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, NoRates, maxAmounts: 1);

        // Assert
        Assert.Contains("> 250 kc", result);
        Assert.DoesNotContain("1000 eur", result);
    }

    [Fact]
    public void FormatMessage_WithEqualAmounts_KeepsTheOneMentionedFirst()
    {
        // Arrange
        var inputs = new[]
        {
            new UserInput(250m, Currency.Czk, "250 kc"),
            new UserInput(250m, Currency.Czk, "250,-")
        };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, NoRates, maxAmounts: 1);

        // Assert
        Assert.Contains("> 250 kc", result);
        Assert.DoesNotContain("250,-", result);
    }

    [Theory]
    [InlineData(Currency.Eur, "za jedno euro.")]
    [InlineData(Currency.Usd, "za jeden dolar.")]
    [InlineData(Currency.Gbp, "za jednu libru.")]
    [InlineData(Currency.Pln, "za jeden zlotý.")]
    public void FormatMessage_WithForeignCurrency_NamesTheCurrencyInExchangeRateNote(Currency currency, string expectedEnding)
    {
        // Arrange
        var inputs = new[] { new UserInput(10m, currency, "10 jednotek") };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, Rate(currency, 25m), MaxAmounts);

        // Assert
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", result);
        Assert.Contains("Při aktuálním směnném kurzu ve výši", result);
        Assert.Contains(expectedEnding, result);
    }

    [Fact]
    public void FormatMessage_WithCzkInput_HasNoExchangeRateNote()
    {
        // Arrange
        var inputs = new[] { new UserInput(250m, Currency.Czk, "250 kc") };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, Rate(Currency.Eur, 25m), MaxAmounts);

        // Assert
        Assert.DoesNotContain("směnném kurzu", result);
    }

    [Fact]
    public void FormatMessage_WithRateForAnotherCurrencyOnly_SkipsTheAmount()
    {
        // Arrange
        var inputs = new[] { new UserInput(10m, Currency.Usd, "10 usd") };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, Rate(Currency.Eur, 25m), MaxAmounts);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FormatMessage_WithSeveralForeignCurrencies_ConvertsEachWithItsOwnRate()
    {
        // Arrange
        var inputs = new[]
        {
            new UserInput(100m, Currency.Pln, "100 zł"), // 600 CZK
            new UserInput(20m, Currency.Usd, "20 usd"), // 400 CZK
            new UserInput(10m, Currency.Gbp, "10 liber") // 300 CZK
        };
        var exchangeRates = new Dictionary<Currency, decimal>
        {
            [Currency.Pln] = 6m,
            [Currency.Usd] = 20m,
            [Currency.Gbp] = 30m
        };

        // Act
        var result = MessageFormatter.FormatMessage(inputs, 100m, exchangeRates, maxAmounts: 2);

        // Assert
        Assert.Contains("> 100 zł", result);
        Assert.Contains("To by stačilo na 6 dvoulitrovek", result);
        Assert.Contains("> 20 usd", result);
        Assert.Contains("To by stačilo na 4 dvoulitrovky", result);
        Assert.DoesNotContain("10 liber", result);
    }
}
