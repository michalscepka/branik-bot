using BranikBot.Parsing;
using BranikBot.Pricing;
using BranikBot.Replies;

namespace BranikBot.Tests.Replies;

public class MessageFormatterTests
{
    [Fact]
    public void FormatMessage_WithEmptyInputs_ReturnsEmptyString()
    {
        // Act
        var result = MessageFormatter.FormatMessage([], 100m, null);

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
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, null);

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
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, null);

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
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, null);

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
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, null);

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
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, null);

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
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, exchangeRate);

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
        var result = MessageFormatter.FormatMessage(inputs, marketPrice, exchangeRate);

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
        var result = MessageFormatter.FormatMessage(inputs, 100m, null);

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
        var result = MessageFormatter.FormatMessage(inputs, 100m, null);

        // Assert
        Assert.Contains("> 250 kc", result);
        Assert.DoesNotContain("10 eur", result);
    }
}
