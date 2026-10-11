using BranikBot.Configuration;

namespace BranikBot.Tests.Configuration;

public class AppVersionTests
{
    [Theory]
    [InlineData("0.2.0", "0.2.0")]
    [InlineData("0.0.0-dev", "0.0.0-dev")]
    [InlineData("0.2.0+63ae3757394dfa977ff1de83e02db1aadf587e18", "0.2.0")]
    [InlineData("0.0.0-dev+63ae375", "0.0.0-dev")]
    public void Parse_InformationalVersion_DropsCommitSuffix(string informationalVersion, string expected)
    {
        // Act
        var version = AppVersion.Parse(informationalVersion);

        // Assert
        Assert.Equal(expected, version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Parse_MissingVersion_ReturnsUnknown(string? informationalVersion)
    {
        // Act
        var version = AppVersion.Parse(informationalVersion);

        // Assert
        Assert.Equal("unknown", version);
    }

    [Fact]
    public void Current_LocalBuild_IsTheDevelopmentVersion()
    {
        Assert.Equal("0.0.0-dev", AppVersion.Current);
    }
}
