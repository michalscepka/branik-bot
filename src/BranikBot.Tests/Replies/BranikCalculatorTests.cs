using BranikBot.Replies;

namespace BranikBot.Tests.Replies;

public class BranikCalculatorTests
{
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(29.99, 0, 0, 0)]
    [InlineData(30, 1, 0, 0)]
    [InlineData(179.99, 5, 0, 0)]
    [InlineData(180, 6, 1, 0)]
    [InlineData(18_000, 600, 100, 1)]
    [InlineData(45_000, 1_500, 250, 2)]
    public void CalculateAmounts_WithAmount_ReturnsBottlesParcelsAndPallets(
        decimal amount, int expectedBottles, int expectedParcels, int expectedPallets)
    {
        // Arrange
        var branikPrice = 30m;

        // Act
        var (bottles, parcels, pallets) = BranikCalculator.CalculateAmounts(amount, branikPrice);

        // Assert
        Assert.Equal(expectedBottles, bottles);
        Assert.Equal(expectedParcels, parcels);
        Assert.Equal(expectedPallets, pallets);
    }

    [Fact]
    public void CalculateAmounts_WithMoreBottlesThanIntCanHold_ReturnsIntMaxBottles()
    {
        // Arrange
        var amount = 100_000_000_000m;
        var branikPrice = 30m;

        // Act
        var (bottles, _, _) = BranikCalculator.CalculateAmounts(amount, branikPrice);

        // Assert
        Assert.Equal(int.MaxValue, bottles);
    }
}
