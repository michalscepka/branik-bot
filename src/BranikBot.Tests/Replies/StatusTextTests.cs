using BranikBot.Replies;

namespace BranikBot.Tests.Replies;

public class StatusTextTests
{
    [Theory]
    [InlineData(37.9, "0.2.0", "Braník za 37,90 Kč · v0.2.0")]
    [InlineData(40, "1.10.3", "Braník za 40,00 Kč · v1.10.3")]
    [InlineData(32.99, "0.0.0-dev", "Braník za 32,99 Kč · v0.0.0-dev")]
    public void Create_WithPrice_ShowsPriceAndVersion(decimal price, string version, string expected)
    {
        // Act
        var text = StatusText.Create(price, version);

        // Assert
        Assert.Equal(expected, text);
    }

    [Fact]
    public void Create_WithoutPrice_ShowsOnlyVersion()
    {
        // Act
        var text = StatusText.Create(null, "0.2.0");

        // Assert
        Assert.Equal("v0.2.0", text);
    }
}
