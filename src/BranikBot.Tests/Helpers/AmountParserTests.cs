using BranikBot.Domain.Services;
using BranikBot.Domain.Enums;
using BranikBot.Domain.Models;
using BranikBot.Infrastructure.Helpers;
using BranikBot.Infrastructure.Services;

namespace BranikBot.Tests.Helpers;

public class AmountParserTests
{
    [Theory]
    // Euro variations
    [InlineData("100 eur", 100, Currency.Eur)]
    [InlineData("100 EUR", 100, Currency.Eur)]
    [InlineData("100 euro", 100, Currency.Eur)]
    [InlineData("100 eura", 100, Currency.Eur)]
    [InlineData("100 €", 100, Currency.Eur)]
    [InlineData("20.5 eur", 20.5, Currency.Eur)]
    [InlineData("0 eur", 0, Currency.Eur)]
    [InlineData("1000000 eur", 1000000, Currency.Eur)]
    // CZK variations
    [InlineData("100 kc", 100, Currency.Czk)]
    [InlineData("100kc", 100, Currency.Czk)]
    [InlineData("100 kč", 100, Currency.Czk)]
    [InlineData("100 ,-", 100, Currency.Czk)]
    [InlineData("100 czk", 100, Currency.Czk)]
    [InlineData("100 korun", 100, Currency.Czk)]
    [InlineData("100 koruny", 100, Currency.Czk)]
    [InlineData("100 koruna", 100, Currency.Czk)]
    [InlineData("100 KC", 100, Currency.Czk)]
    [InlineData("1 000 kc", 1000, Currency.Czk)]
    // Multipliers (k, mega)
    [InlineData("2 k", 2000, Currency.Czk)]
    [InlineData("1 mega", 1000000, Currency.Czk)]
    [InlineData("1.5 k", 1500, Currency.Czk)]
    [InlineData("1,5 k", 1500, Currency.Czk)]
    [InlineData("0,5 k", 500, Currency.Czk)]
    // Multipliers with Currency
    [InlineData("5k euro", 5000, Currency.Eur)]
    [InlineData("5k eur", 5000, Currency.Eur)]
    [InlineData("1.2 mega eura", 1200000, Currency.Eur)]
    public void ExtractPrices_ValidSingleInput_ReturnsCorrectResult(string input, decimal expectedAmount, Currency expectedCurrency)
    {
        var result = input.ExtractAmounts();

        Assert.Single(result);
        var price = result.First();
        Assert.Equal(expectedAmount, price.Amount);
        Assert.Equal(expectedCurrency, price.Currency);
        Assert.Equal(input, price.OriginalText);
    }

    [Fact]
    public void ExtractPrices_MultiplePrices_ReturnsAll()
    {
        var input = "It costs 100 kc and 2k for shipping.";

        var result = input.ExtractAmounts();

        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Amount == 100m && p.Currency == Currency.Czk);
        Assert.Contains(result, p => p.Amount == 2000m && p.Currency == Currency.Czk);
    }

    [Fact]
    public void ExtractPrices_NoPrice_ReturnsEmpty()
    {
        var input = "This is a random text without price.";

        var result = input.ExtractAmounts();

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("100")] // Missing currency
    [InlineData("kc 100")] // Wrong order
    [InlineData("just text")]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractPrices_InvalidFormat_ReturnsEmpty(string input)
    {
         var result = input.ExtractAmounts();
         Assert.Empty(result);
    }

    [Theory]
    [InlineData("Stojí to 200 kč.", 200)]
    [InlineData("Cena je 1500,- a doprava zdarma.", 1500)]
    [InlineData("Mám tu na prodej auto za 50 000 kč, sleva možná.", 50000)]
    [InlineData("Stojí to 5,5k.", 5500)]
    [InlineData("Cena: 1.2 mega.", 1200000)]
    [InlineData("To je drahý, dám ti za to maximálně 500 korun.", 500)]
    [InlineData("Ahoj, prodám iPhone 15 za 25 000,-. Je to super cena.", 25000)]
    [InlineData("Na účtě mám 0,05 mega.", 50000)]
    [InlineData("Dlužím ti 1000czk.", 1000)]
    public void ExtractPrices_RealWorldSentences_FindsExpectedPrice(string input, decimal expectedPrice)
    {
        var result = input.ExtractAmounts();
        Assert.Contains(result, p => p.Amount == expectedPrice);
    }

    [Fact]
    public void ExtractPrices_ComplexSentence_ReturnsMultiplePrices()
    {
        var input = "Dám ti 5k a ty mi vrátíš 200 kč, platí?";
        var result = input.ExtractAmounts();

        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Amount == 5000 && p.Currency == Currency.Czk);
        Assert.Contains(result, p => p.Amount == 200 && p.Currency == Currency.Czk);
    }
}
