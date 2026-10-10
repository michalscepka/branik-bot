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

    [Fact]
    public void GetOneUnit_HomeCurrency_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Messages.GetOneUnit(Currency.Czk));
    }

    private static int CountOf(string value, string text) => text.Split(value).Length - 1;
}
