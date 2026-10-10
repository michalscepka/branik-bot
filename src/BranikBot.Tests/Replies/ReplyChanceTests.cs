using BranikBot.Replies;

namespace BranikBot.Tests.Replies;

public class ReplyChanceTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.24)]
    public void Roll_DrawBelowChance_ReturnsTrue(double draw)
    {
        // Arrange
        var chance = new ReplyChance(0.25, new FixedRandom(draw));

        // Act
        var replies = chance.Roll();

        // Assert
        Assert.True(replies);
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(0.99)]
    public void Roll_DrawAtOrAboveChance_ReturnsFalse(double draw)
    {
        // Arrange
        var chance = new ReplyChance(0.25, new FixedRandom(draw));

        // Act
        var replies = chance.Roll();

        // Assert
        Assert.False(replies);
    }

    [Fact]
    public void Roll_ChanceOfOne_ReturnsTrueForHighestDraw()
    {
        // Arrange
        var chance = new ReplyChance(1.0, new FixedRandom(0.999999));

        // Act
        var replies = chance.Roll();

        // Assert
        Assert.True(replies);
    }

    [Fact]
    public void Roll_ChanceOfZero_ReturnsFalseForLowestDraw()
    {
        // Arrange
        var chance = new ReplyChance(0.0, new FixedRandom(0.0));

        // Act
        var replies = chance.Roll();

        // Assert
        Assert.False(replies);
    }

    private sealed class FixedRandom(double draw) : Random
    {
        public override double NextDouble() => draw;
    }
}
