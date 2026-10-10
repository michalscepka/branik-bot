using BranikBot.Parsing;
using BranikBot.Pricing;

namespace BranikBot.Tests.Parsing;

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

    [Theory]
    // Thousands
    [InlineData("20 tisíc", 20_000)]
    [InlineData("3 tisíce", 3_000)]
    [InlineData("5 tisíců", 5_000)]
    [InlineData("20 tisic", 20_000)]
    [InlineData("3 tisice", 3_000)]
    [InlineData("5 tisicu", 5_000)]
    [InlineData("20 tis", 20_000)]
    [InlineData("20 tis.", 20_000)]
    [InlineData("20tis.", 20_000)]
    // Millions
    [InlineData("1 milion", 1_000_000)]
    [InlineData("2 miliony", 2_000_000)]
    [InlineData("5 milionů", 5_000_000)]
    [InlineData("5 milionu", 5_000_000)]
    [InlineData("1 milión", 1_000_000)]
    [InlineData("2 milióny", 2_000_000)]
    [InlineData("5 miliónů", 5_000_000)]
    [InlineData("3 mil", 3_000_000)]
    [InlineData("3 mil.", 3_000_000)]
    [InlineData("3 mio", 3_000_000)]
    [InlineData("3 mio.", 3_000_000)]
    // Billions
    [InlineData("1 miliarda", 1_000_000_000)]
    [InlineData("2 miliardy", 2_000_000_000)]
    [InlineData("5 miliard", 5_000_000_000)]
    [InlineData("3 mld", 3_000_000_000)]
    [InlineData("3 mld.", 3_000_000_000)]
    // Decimals, grouped digits and casing
    [InlineData("1,5 milionu", 1_500_000)]
    [InlineData("1.5 mld", 1_500_000_000)]
    [InlineData("0,5 tisíce", 500)]
    [InlineData("1 500 tisíc", 1_500_000)]
    [InlineData("20 TISÍC", 20_000)]
    [InlineData("2 Miliony", 2_000_000)]
    [InlineData("3 MLD.", 3_000_000_000)]
    public void ExtractAmounts_MultiplierInWords_ReturnsMultipliedCzkAmount(string input, decimal expectedAmount)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(expectedAmount, amount.Amount);
        Assert.Equal(Currency.Czk, amount.Currency);
        Assert.Equal(input, amount.OriginalText);
    }

    [Theory]
    [InlineData("20 tisíc korun", 20_000, Currency.Czk)]
    [InlineData("20 tisíc Kč", 20_000, Currency.Czk)]
    [InlineData("1,5 mil. Kč", 1_500_000, Currency.Czk)]
    [InlineData("1,5 mil.Kč", 1_500_000, Currency.Czk)]
    [InlineData("20 tisíc euro", 20_000, Currency.Eur)]
    [InlineData("2 miliony eur", 2_000_000, Currency.Eur)]
    [InlineData("2 mil. €", 2_000_000, Currency.Eur)]
    [InlineData("3 mld eur", 3_000_000_000, Currency.Eur)]
    [InlineData("3 miliardy€", 3_000_000_000, Currency.Eur)]
    public void ExtractAmounts_MultiplierInWordsWithCurrency_ReturnsCurrencyAndWholeText(
        string input, decimal expectedAmount, Currency expectedCurrency)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(expectedAmount, amount.Amount);
        Assert.Equal(expectedCurrency, amount.Currency);
        Assert.Equal(input, amount.OriginalText);
    }

    [Theory]
    [InlineData("Barák stál 5 milionů.", "5 milionů")]
    [InlineData("Dal za to 20 tisíc, fakt.", "20 tisíc")]
    [InlineData("Dluh je 3 miliardy!", "3 miliardy")]
    [InlineData("Stojí to 5k.", "5k")]
    [InlineData("Rozpočet 1,5 mil. Kč je málo.", "1,5 mil. Kč")]
    [InlineData("Má to cenu 20 tis. a víc ne.", "20 tis.")]
    public void ExtractAmounts_MultiplierInsideSentence_QuotesOnlyTheAmount(string input, string expectedText)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(expectedText, amount.OriginalText);
    }

    [Theory]
    [InlineData("5 tisíciletí")] // Millennia
    [InlineData("3 milí lidé")]
    [InlineData("3 milenci")]
    [InlineData("2 miliardáři")]
    [InlineData("4 megabajty")]
    [InlineData("7 kilo")]
    [InlineData("tisíc")] // No number
    [InlineData("pár milionů")]
    [InlineData("mld 3")] // Wrong order
    public void ExtractAmounts_WordThatOnlyStartsLikeMultiplier_ReturnsEmpty(string input)
    {
        var result = input.ExtractAmounts();

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractAmounts_SeveralMultipliersInOneMessage_ReturnsAll()
    {
        var input = "Byt za 5 milionů, auto za 300 tisíc a firma za 2 mld eur.";

        var result = input.ExtractAmounts().ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, a => a is { Amount: 5_000_000, Currency: Currency.Czk });
        Assert.Contains(result, a => a is { Amount: 300_000, Currency: Currency.Czk });
        Assert.Contains(result, a => a is { Amount: 2_000_000_000, Currency: Currency.Eur });
    }

    [Fact]
    public void ExtractAmounts_SameAmountWrittenTwice_ReturnsItOnce()
    {
        var input = "20 tisíc sem, 20 tisíc tam";

        var result = input.ExtractAmounts();

        Assert.Single(result);
    }

    [Theory]
    [InlineData("999 999 999 999 999 999 999 999 999 mld")]
    [InlineData("9 999 999 999 999 999 999 999 mega")]
    [InlineData("99 999 999 999 999 999 999 999 999 kč")]
    public void ExtractAmounts_AmountTooLargeToMultiply_ReturnsCappedAmount(string input)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(1_000_000_000_000_000_000m, amount.Amount);
    }

    [Theory]
    [InlineData("1 dvacka", 20)]
    [InlineData("2 dvacky", 40)]
    [InlineData("10 dvacek", 200)]
    [InlineData("1 stovka", 100)]
    [InlineData("2 stovky", 200)]
    [InlineData("5 stovek", 500)]
    [InlineData("5stovek", 500)]
    [InlineData("4 dvoustovky", 800)]
    [InlineData("5 dvoustovek", 1_000)]
    [InlineData("2 pětistovky", 1_000)]
    [InlineData("6 pětistovek", 3_000)]
    [InlineData("3 tisícovky", 3_000)]
    [InlineData("10 tisícovek", 10_000)]
    [InlineData("3 dvoutisícovky", 6_000)]
    [InlineData("5 dvoutisícovek", 10_000)]
    [InlineData("2 pětitisícovky", 10_000)]
    [InlineData("5 pětitisícovek", 25_000)]
    [InlineData("1 tác", 1_000)]
    [InlineData("2 táce", 2_000)]
    [InlineData("5 táců", 5_000)]
    [InlineData("1 meloun", 1_000_000)]
    [InlineData("3 melouny", 3_000_000)]
    [InlineData("10 melounů", 10_000_000)]
    [InlineData("1,5 melounu", 1_500_000)]
    public void ExtractAmounts_NumberWithSlang_ReturnsMultipliedCzkAmount(string input, decimal expectedAmount)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(expectedAmount, amount.Amount);
        Assert.Equal(Currency.Czk, amount.Currency);
        Assert.Equal(input, amount.OriginalText);
    }

    [Theory]
    [InlineData("dvacka", 20)]
    [InlineData("dvacku", 20)]
    [InlineData("stovka", 100)]
    [InlineData("stovku", 100)]
    [InlineData("dvoustovka", 200)]
    [InlineData("dvoustovku", 200)]
    [InlineData("pětistovka", 500)]
    [InlineData("pětistovku", 500)]
    [InlineData("tisícovka", 1_000)]
    [InlineData("tisícovku", 1_000)]
    [InlineData("dvoutisícovka", 2_000)]
    [InlineData("dvoutisícovku", 2_000)]
    [InlineData("pětitisícovka", 5_000)]
    [InlineData("pětitisícovku", 5_000)]
    [InlineData("STOVKA", 100)]
    [InlineData("Pětistovka", 500)]
    public void ExtractAmounts_SlangWithoutNumber_ReturnsOneDenomination(string input, decimal expectedAmount)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(expectedAmount, amount.Amount);
        Assert.Equal(Currency.Czk, amount.Currency);
        Assert.Equal(input, amount.OriginalText);
    }

    [Theory]
    [InlineData("stovka euro", 100, Currency.Eur)]
    [InlineData("pětistovku eur", 500, Currency.Eur)]
    [InlineData("5 stovek euro", 500, Currency.Eur)]
    [InlineData("2 táce eur", 2_000, Currency.Eur)]
    [InlineData("3 melouny €", 3_000_000, Currency.Eur)]
    [InlineData("2 stovky korun", 200, Currency.Czk)]
    public void ExtractAmounts_SlangWithCurrency_ReturnsCurrencyAndWholeText(
        string input, decimal expectedAmount, Currency expectedCurrency)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(expectedAmount, amount.Amount);
        Assert.Equal(expectedCurrency, amount.Currency);
        Assert.Equal(input, amount.OriginalText);
    }

    [Theory]
    [InlineData("Půjč mi stovku, prosím.", "stovku", 100)]
    [InlineData("Stálo to pětistovku.", "pětistovku", 500)]
    [InlineData("To je tisícovka!", "tisícovka", 1_000)]
    [InlineData("Dal jsem za to 2 táce a nelituju.", "2 táce", 2_000)]
    [InlineData("Barák za 8 melounů, no nekup to.", "8 melounů", 8_000_000)]
    public void ExtractAmounts_SlangInsideSentence_QuotesOnlyTheAmount(
        string input, string expectedText, decimal expectedAmount)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(expectedText, amount.OriginalText);
        Assert.Equal(expectedAmount, amount.Amount);
    }

    [Theory]
    // Plurals without a number are not an amount
    [InlineData("stovky lidí")]
    [InlineData("tisícovky fanoušků")]
    [InlineData("pár dvacek")]
    // Fruit and trays
    [InlineData("koupil jsem meloun")]
    [InlineData("melouny jsou v akci")]
    [InlineData("podej mi tác")]
    [InlineData("2 tácky")]
    [InlineData("3 melounové dorty")]
    // Longer words that contain a slang word
    [InlineData("stovkař")]
    [InlineData("šestistovka")]
    [InlineData("tisícovkami")]
    // Left out on purpose
    [InlineData("pade")]
    [InlineData("při pádě")]
    [InlineData("kilo")]
    [InlineData("litr")]
    public void ExtractAmounts_SlangLookAlike_ReturnsEmpty(string input)
    {
        var result = input.ExtractAmounts();

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractAmounts_SlangWithAndWithoutNumber_ReturnsBoth()
    {
        var input = "Dlužíš mi stovku a já tobě 3 stovky.";

        var result = input.ExtractAmounts().ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a is { Amount: 100, OriginalText: "stovku" });
        Assert.Contains(result, a => a is { Amount: 300, OriginalText: "3 stovky" });
    }

    [Theory]
    [InlineData("20 tisic", 20_000)]
    [InlineData("5 milionu", 5_000_000)]
    [InlineData("5 MILIONŮ", 5_000_000)]
    [InlineData("100 Kc", 100)]
    [InlineData("100 KČ", 100)]
    [InlineData("petistovka", 500)]
    [InlineData("3 tisicovky", 3_000)]
    [InlineData("5 tacu", 5_000)]
    [InlineData("10 melounu", 10_000_000)]
    public void ExtractAmounts_WithOrWithoutDiacritics_ReturnsSameAmountAndKeepsOriginalText(
        string input, decimal expectedAmount)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(expectedAmount, amount.Amount);
        Assert.Equal(input, amount.OriginalText);
    }

    [Theory]
    [InlineData("🍺 dej mi stovku 🍺", "stovku")]
    [InlineData("🍺🍺🍺 20 tisíc Kč 🎉", "20 tisíc Kč")]
    [InlineData("Žluťoučký kůň úpěl za 5 táců", "5 táců")]
    public void ExtractAmounts_MessageWithEmojiOrAccents_QuotesTextFromTheOriginalMessage(
        string input, string expectedText)
    {
        var result = input.ExtractAmounts();

        var amount = Assert.Single(result);
        Assert.Equal(expectedText, amount.OriginalText);
    }
}
