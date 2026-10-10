using BranikBot.Replies;

namespace BranikBot.Tests.Replies;

public class ExtraFactsTests
{
    private const decimal BottlePrice = 30m;

    private const int NextBottle = 0;
    private const int HalfLitre = 1;
    private const int NextUnit = 2;
    private const int Change = 3;

    [Theory]
    [InlineData(40, 1, "Do další ti chybí 20,00 Kč.")]
    [InlineData(59.99, 1, "Do další ti chybí 0,01 Kč.")]
    [InlineData(42.5, 1, "Do další ti chybí 17,50 Kč.")]
    [InlineData(30, 1, "Do další ti chybí 30,00 Kč.")] // Nothing left over, so a whole bottle is missing
    [InlineData(3_000, 100, "Do další ti chybí 30,00 Kč.")]
    public void ForConversion_AnyAmount_SaysWhatIsMissingForOneMoreBottle(
        decimal valueInCzk, int bottleCount, string expectedFact)
    {
        var facts = ExtraFacts.ForConversion(bottleCount, valueInCzk, BottlePrice);

        Assert.Equal(expectedFact, facts[NextBottle]);
    }

    [Theory]
    [InlineData(30, "Půllitr tě tak vyjde na 7,50 Kč.")]
    [InlineData(32.9, "Půllitr tě tak vyjde na 8,23 Kč.")]
    [InlineData(100, "Půllitr tě tak vyjde na 25,00 Kč.")]
    [InlineData(0.01, "Půllitr tě tak vyjde na 0,00 Kč.")]
    public void ForConversion_AnyPrice_SaysThePriceOfOneHalfLitre(decimal bottlePrice, string expectedFact)
    {
        var facts = ExtraFacts.ForConversion(1, bottlePrice, bottlePrice);

        Assert.Equal(expectedFact, facts[HalfLitre]);
    }

    [Theory]
    [InlineData(1, "Na další balík potřebuješ ještě 5 dvoulitrovek.")]
    [InlineData(2, "Na další balík potřebuješ ještě 4 dvoulitrovky.")]
    [InlineData(4, "Na další balík potřebuješ ještě 2 dvoulitrovky.")]
    [InlineData(5, "Na další balík potřebuješ ještě 1 dvoulitrovku.")]
    [InlineData(6, "Na další balík potřebuješ ještě 6 dvoulitrovek.")] // A full parcel needs a whole new one
    [InlineData(7, "Na další balík potřebuješ ještě 5 dvoulitrovek.")]
    [InlineData(99, "Na další balík potřebuješ ještě 3 dvoulitrovky.")]
    public void ForConversion_FewerBottlesThanCountedInParcels_SaysBottlesMissingForNextParcel(
        int bottleCount, string expectedFact)
    {
        var facts = ExtraFacts.ForConversion(bottleCount, bottleCount * BottlePrice, BottlePrice);

        Assert.Equal(expectedFact, facts[NextUnit]);
    }

    [Theory]
    [InlineData(100, "Na další europaletu potřebuješ ještě 84 baliku.")] // 16 parcels
    [InlineData(120, "Na další europaletu potřebuješ ještě 80 baliku.")] // 20 parcels
    [InlineData(588, "Na další europaletu potřebuješ ještě 2 baliky.")] // 98 parcels
    [InlineData(594, "Na další europaletu potřebuješ ještě 1 balik.")] // 99 parcels
    [InlineData(600, "Na další europaletu potřebuješ ještě 100 baliku.")] // A full pallet needs a whole new one
    [InlineData(1_200, "Na další europaletu potřebuješ ještě 100 baliku.")]
    [InlineData(1_506, "Na další europaletu potřebuješ ještě 49 baliku.")] // 251 parcels
    [InlineData(99_999, "Na další europaletu potřebuješ ještě 34 baliku.")] // 16 666 parcels
    public void ForConversion_BottlesCountedInParcels_SaysParcelsMissingForNextPallet(
        int bottleCount, string expectedFact)
    {
        var facts = ExtraFacts.ForConversion(bottleCount, bottleCount * BottlePrice, BottlePrice);

        Assert.Equal(expectedFact, facts[NextUnit]);
    }

    [Theory]
    [InlineData(100, 3, "A ještě ti zbyde 10,00 Kč.")]
    [InlineData(42.5, 1, "A ještě ti zbyde 12,50 Kč.")]
    [InlineData(30.01, 1, "A ještě ti zbyde 0,01 Kč.")]
    [InlineData(1_029.99, 34, "A ještě ti zbyde 9,99 Kč.")]
    public void ForConversion_MoneyLeftOver_SaysTheChange(decimal valueInCzk, int bottleCount, string expectedFact)
    {
        var facts = ExtraFacts.ForConversion(bottleCount, valueInCzk, BottlePrice);

        Assert.Equal(4, facts.Count);
        Assert.Equal(expectedFact, facts[Change]);
    }

    [Theory]
    [InlineData(30, 1)] // Exact price
    [InlineData(300, 10)]
    [InlineData(30.004, 1)] // Less than one haléř left
    public void ForConversion_NothingLeftOver_HasNoChangeFact(decimal valueInCzk, int bottleCount)
    {
        var facts = ExtraFacts.ForConversion(bottleCount, valueInCzk, BottlePrice);

        Assert.Equal(3, facts.Count);
        Assert.DoesNotContain(facts, fact => fact.Contains("zbyde"));
    }

    [Theory]
    [InlineData(40, 1)]
    [InlineData(100, 3)]
    [InlineData(42.5, 1)]
    public void ForConversion_MoneyLeftOver_ChangeAndMissingAddUpToOneBottle(decimal valueInCzk, int bottleCount)
    {
        // Arrange
        var change = valueInCzk - bottleCount * BottlePrice;

        // Act
        var facts = ExtraFacts.ForConversion(bottleCount, valueInCzk, BottlePrice);

        // Assert
        Assert.Equal($"A ještě ti zbyde {change:C2}.", facts[Change]);
        Assert.Equal($"Do další ti chybí {BottlePrice - change:C2}.", facts[NextBottle]);
    }

    [Fact]
    public void ForConversion_MostBottlesAnIntCanHold_DoesNotThrow()
    {
        var facts = ExtraFacts.ForConversion(int.MaxValue, int.MaxValue * 0.01m, 0.01m);

        Assert.Equal(3, facts.Count);
        Assert.Equal("Na další europaletu potřebuješ ještě 59 baliku.", facts[NextUnit]);
    }

    [Theory]
    [InlineData(5, "Chybí ti ještě 25,00 Kč.")]
    [InlineData(0, "Chybí ti ještě 30,00 Kč.")]
    [InlineData(29.99, "Chybí ti ještě 0,01 Kč.")]
    [InlineData(17.5, "Chybí ti ještě 12,50 Kč.")]
    public void ForInsufficientFunds_AmountBelowPrice_SaysHowMuchIsMissing(decimal valueInCzk, string expectedFact)
    {
        var fact = ExtraFacts.ForInsufficientFunds(valueInCzk, BottlePrice);

        Assert.Equal(expectedFact, fact);
    }

    [Fact]
    public void ForInsufficientFunds_LessThanOneHalerMissing_ReturnsNull()
    {
        var fact = ExtraFacts.ForInsufficientFunds(29.999m, BottlePrice);

        Assert.Null(fact);
    }
}
