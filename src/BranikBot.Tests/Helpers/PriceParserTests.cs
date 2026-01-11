using BranikBot.Infrastructure.Helpers;
using BranikBot.Infrastructure.Enums;

namespace BranikBot.Tests.Helpers;

public class PriceParserTests
{
    [Theory]
    [InlineData("100 eur", 100)]
    [InlineData("100 EUR", 100)]
    [InlineData("100 euro", 100)]
    [InlineData("100 €", 100)]
    [InlineData("20.5 eur", 20.5)]
    public void ExtractPrices_EuroPrices_ReturnsCorrectCurrency(string input, decimal expectedValue)
    {
        var result = input.ExtractPrices();

        Assert.Single(result);
        var price = result.First();
        Assert.Equal(expectedValue, price.Value);
        Assert.Equal(Currency.Eur, price.Currency);
    }

    [Theory]
    [InlineData("100 kc", 100)]
    [InlineData("100kc", 100)]
    [InlineData("100 kč", 100)]
    [InlineData("100 ,-", 100)]
    [InlineData("100 czk", 100)]
    [InlineData("100 korun", 100)]
    [InlineData("100 koruny", 100)]
    [InlineData("100 koruna", 100)]
    [InlineData("100 KC", 100)]
    [InlineData("1 000 kc", 1000)]
    [InlineData("2 k", 2000)]
    [InlineData("1 mega", 1000000)]
    public void ExtractPrices_SinglePrice_ReturnsCorrectValue(string input, decimal expectedValue)
    {
        var result = input.ExtractPrices();

        Assert.Single(result);
        Assert.Equal(expectedValue, result.First().Value);
    }

    [Theory]
    [InlineData("1.5 k", 1500)]
    [InlineData("1,5 k", 1500)]
    public void ExtractPrices_DecimalThousands_ReturnsCorrectValue(string input, decimal expectedValue)
    {
         var result = input.ExtractPrices();

         Assert.Equal(expectedValue, result.First().Value);
    }

    [Fact]
    public void ExtractPrices_MultiplePrices_ReturnsAll()
    {
        var input = "It costs 100 kc and 2k for shipping.";

        var result = input.ExtractPrices();

        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Value == 100m);
        Assert.Contains(result, p => p.Value == 2000m);
    }

    [Fact]
    public void ExtractPrices_NoPrice_ReturnsEmpty()
    {
        var input = "This is a random text without price.";

        var result = input.ExtractPrices();

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("100", 0)] // Missing currency
    [InlineData("kc 100", 0)] // Wrong order
    public void ExtractPrices_InvalidFormat_ReturnsEmpty(string input, int expectedCount)
    {
         var result = input.ExtractPrices();
         Assert.Equal(expectedCount, result.Count());
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
    public void ExtractPrices_RealLifeSentences_ContainsExpectedPrice(string input, decimal expectedPrice)
    {
        var result = input.ExtractPrices();
        Assert.Contains(result, p => p.Value == expectedPrice);
    }

    [Fact]
    public void ExtractPrices_ComplexSentence_ReturnsMultiplePrices()
    {
        var input = "Dám ti 5k a ty mi vrátíš 200 kč, platí?";
        var result = input.ExtractPrices();

        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.Value == 5000);
        Assert.Contains(result, p => p.Value == 200);
    }
}
