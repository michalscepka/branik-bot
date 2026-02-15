using BranikBot.Application.Providers;
using BranikBot.Domain.Enums;
using BranikBot.Domain.Models;
using BranikBot.Infrastructure.Formatting;
using Moq;

namespace BranikBot.Tests.Formatting;

public class MessageFormatterTests
{
    private readonly Mock<IExchangeRateProvider> _exchangeRateProviderMock;
    private readonly MessageFormatter _formatter;

    public MessageFormatterTests()
    {
        _exchangeRateProviderMock = new Mock<IExchangeRateProvider>();
        _formatter = new MessageFormatter(_exchangeRateProviderMock.Object);
    }

    [Fact]
    public async Task FormatMessageAsync_WithEmptyInputs_ReturnsEmptyString()
    {
        // Act
        var result = await _formatter.FormatMessageAsync([], 100m);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FormatMessageAsync_WithCzkInput_InsufficientFunds_ReturnsInsufficientFundsMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(50m, Currency.Czk, "50 kc") };
        var marketPrice = 100m;

        // Act
        var result = await _formatter.FormatMessageAsync(inputs, marketPrice);

        // Assert
        Assert.Contains("Lítost má v srdci, ale 50 kc nepostačí ani na jednu číši Braníčka plastového ve slevě.", result);
    }

    [Fact]
    public async Task FormatMessageAsync_WithCzkInput_SufficientFunds_LessThen100Bottles_ReturnsBottleMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(250m, Currency.Czk, "250 kc") };
        var marketPrice = 100m; // 2 bottles

        // Act
        var result = await _formatter.FormatMessageAsync(inputs, marketPrice);

        // Assert
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", result);
    }

    [Fact]
    public async Task FormatMessageAsync_WithCzkInput_SufficientFunds_Between100And999Bottles_ReturnsParcelMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(12000m, Currency.Czk, "12000 kc") };
        var marketPrice = 100m; // 120 bottles = 20 parcels

        // Act
        var result = await _formatter.FormatMessageAsync(inputs, marketPrice);

        // Assert
        Assert.Contains("To by stačilo na 20 baliku (120 dvoulitrovek) Braníčka ve slevě!", result);
    }

    [Fact]
    public async Task FormatMessageAsync_WithCzkInput_SufficientFunds_Between1000And100000Bottles_ReturnsPalletMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(120000m, Currency.Czk, "120000 kc") };
        var marketPrice = 100m; // 1200 bottles = 200 parcels = 2 pallets

        // Act
        var result = await _formatter.FormatMessageAsync(inputs, marketPrice);

        // Assert
        Assert.Contains("To by stačilo na 2 europalety (200 baliku) dvoulitrovek Braníčka ve slevě!", result);
    }

    [Fact]
    public async Task FormatMessageAsync_WithCzkInput_TooRich_ReturnsTooRichMessage()
    {
        // Arrange
        var inputs = new[] { new UserInput(20_000_000m, Currency.Czk, "20M kc") };
        var marketPrice = 100m; // 200,000 bottles > 100,000

        // Act
        var result = await _formatter.FormatMessageAsync(inputs, marketPrice);

        // Assert
        Assert.Contains("Za tolik by sis mohl koupit rovnou celý Plzeňský Prazdroj!", result);
    }

    [Fact]
    public async Task FormatMessageAsync_WithEurInput_CallsExchangeRateProvider()
    {
        // Arrange
        var inputs = new[] { new UserInput(10m, Currency.Eur, "10 eur") };
        var marketPrice = 100m;
        var exchangeRate = 25m; // 10 EUR * 25 = 250 CZK = 2 bottles

        _exchangeRateProviderMock
            .Setup(p => p.GetExchangeRateAsync(Currency.Eur))
            .ReturnsAsync(exchangeRate);

        // Act
        var result = await _formatter.FormatMessageAsync(inputs, marketPrice);

        // Assert
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", result);
        Assert.Contains("Při aktuálním směnném kurzu ve výši", result);
        Assert.Contains("25", result);
        Assert.Contains("za jedno euro.", result);
        _exchangeRateProviderMock.Verify(p => p.GetExchangeRateAsync(Currency.Eur), Times.Once);
    }

    [Fact]
    public async Task FormatMessageAsync_WithMultipleInputs_ReturnsAllLines()
    {
        // Arrange
        var inputs = new[]
        {
            new UserInput(250m, Currency.Czk, "250 kc"),
            new UserInput(10m, Currency.Eur, "10 eur")
        };
        var marketPrice = 100m;
        var exchangeRate = 25m;

        _exchangeRateProviderMock
            .Setup(p => p.GetExchangeRateAsync(Currency.Eur))
            .ReturnsAsync(exchangeRate);

        // Act
        var result = await _formatter.FormatMessageAsync(inputs, marketPrice);

        // Assert
        Assert.Contains("> 250 kc", result);
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", result);
        Assert.Contains("> 10 eur", result);
        Assert.Contains("To by stačilo na 2 dvoulitrovky Braníčka ve slevě!", result);
        Assert.Contains("Při aktuálním směnném kurzu ve výši", result);
    }

    [Fact]
    public async Task FormatMessageAsync_WithMultipleEurInputs_CallsExchangeRateProviderOnlyOnce()
    {
        // Arrange
        var inputs = new[]
        {
            new UserInput(10m, Currency.Eur, "10 eur"),
            new UserInput(20m, Currency.Eur, "20 eur")
        };
        var marketPrice = 100m;
        var exchangeRate = 25m;

        _exchangeRateProviderMock
            .Setup(p => p.GetExchangeRateAsync(Currency.Eur))
            .ReturnsAsync(exchangeRate);

        // Act
        await _formatter.FormatMessageAsync(inputs, marketPrice);

        // Assert
        _exchangeRateProviderMock.Verify(p => p.GetExchangeRateAsync(Currency.Eur), Times.Once);
    }
}
