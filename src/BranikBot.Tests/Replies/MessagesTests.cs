using BranikBot.Pricing;
using BranikBot.Replies;

namespace BranikBot.Tests.Replies;

public class MessagesTests
{
    public static TheoryData<string> ConversionPhrasings => [.. Messages.Conversion];

    public static TheoryData<string> InsufficientFundsPhrasings => [.. Messages.InsufficientFunds];

    public static TheoryData<string> TooRichPhrasings => [.. Messages.TooRich];

    public static TheoryData<string> AllPhrasings =>
        [.. Messages.Conversion, .. Messages.InsufficientFunds, .. Messages.TooRich];

    [Theory]
    [MemberData(nameof(ConversionPhrasings))]
    [MemberData(nameof(InsufficientFundsPhrasings))]
    public void Phrasing_WithPlaceholder_HasItExactlyOnce(string phrasing)
    {
        // Act
        var formatted = string.Format(phrasing, "<value>");

        // Assert
        Assert.Equal(1, CountOf("<value>", formatted));
        Assert.DoesNotContain("{", formatted);
    }

    [Theory]
    [MemberData(nameof(TooRichPhrasings))]
    public void Phrasing_ForHugeAmount_HasNoPlaceholder(string phrasing)
    {
        Assert.DoesNotContain("{", phrasing);
    }

    [Theory]
    [MemberData(nameof(AllPhrasings))]
    public void Phrasing_Any_IsOneLineWithoutEmDash(string phrasing)
    {
        Assert.False(string.IsNullOrWhiteSpace(phrasing));
        Assert.DoesNotContain("\n", phrasing);
        Assert.DoesNotContain("\u2014", phrasing);
    }

    [Fact]
    public void Phrasings_InEachList_AreSeveralAndDistinct()
    {
        Assert.All(new[] { Messages.Conversion, Messages.InsufficientFunds, Messages.TooRich }, phrasings =>
        {
            Assert.True(phrasings.Count > 1);
            Assert.Equal(phrasings.Count, phrasings.Distinct().Count());
        });
    }

    [Theory]
    [InlineData(Currency.Eur, "jedno euro")]
    [InlineData(Currency.Usd, "jeden dolar")]
    [InlineData(Currency.Gbp, "jednu libru")]
    [InlineData(Currency.Pln, "jeden zlotý")]
    public void GetOneUnit_ForeignCurrency_ReturnsCzechName(Currency currency, string expected)
    {
        Assert.Equal(expected, Messages.GetOneUnit(currency));
    }

    [Theory]
    [InlineData(Messages.ChangeFact, 1)]
    [InlineData(Messages.NextBottleFact, 1)]
    [InlineData(Messages.HalfLitreFact, 1)]
    [InlineData(Messages.MissingFact, 1)]
    [InlineData(Messages.NextParcelFact, 2)]
    [InlineData(Messages.NextPalletFact, 2)]
    public void Fact_Any_UsesEachOfItsPlaceholdersOnce(string fact, int placeholderCount)
    {
        // Act
        var formatted = string.Format(fact, "<first>", "<second>");

        // Assert
        Assert.Equal(1, CountOf("<first>", formatted));
        Assert.Equal(placeholderCount - 1, CountOf("<second>", formatted));
        Assert.DoesNotContain("{", formatted);
        Assert.EndsWith(".", formatted);
    }

    [Theory]
    [InlineData(37.9, "Braník za 37,90 Kč")]
    [InlineData(40, "Braník za 40,00 Kč")]
    public void Status_Price_IsFormattedAsCzechMoney(decimal price, string expected)
    {
        Assert.Equal(expected, string.Format(Messages.Status, price));
    }

    [Fact]
    public void GetOneUnit_HomeCurrency_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Messages.GetOneUnit(Currency.Czk));
    }

    private static int CountOf(string value, string text) => text.Split(value).Length - 1;
}
