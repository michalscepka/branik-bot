using BranikBot.Replies;
using Moq;

namespace BranikBot.Tests.Replies;

public class ChannelCooldownTests
{
    private const ulong ChannelId = 1;

    private static readonly DateTimeOffset Start = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly ChannelCooldown _cooldown;

    public ChannelCooldownTests()
    {
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Start);
        _cooldown = new ChannelCooldown(TimeSpan.FromMinutes(5), _timeProviderMock.Object);
    }

    [Fact]
    public void IsActive_NeverStarted_ReturnsFalse()
    {
        // Act
        var active = _cooldown.IsActive(ChannelId);

        // Assert
        Assert.False(active);
    }

    [Fact]
    public void IsActive_WithinCooldown_ReturnsTrue()
    {
        // Arrange
        _cooldown.TryStart(ChannelId);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Start.AddMinutes(4));

        // Act
        var active = _cooldown.IsActive(ChannelId);

        // Assert
        Assert.True(active);
    }

    [Fact]
    public void IsActive_AfterCooldownElapsed_ReturnsFalse()
    {
        // Arrange
        _cooldown.TryStart(ChannelId);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Start.AddMinutes(5));

        // Act
        var active = _cooldown.IsActive(ChannelId);

        // Assert
        Assert.False(active);
    }

    [Fact]
    public void IsActive_NeverStarted_DoesNotStartCooldown()
    {
        // Arrange
        _cooldown.IsActive(ChannelId);

        // Act
        var started = _cooldown.TryStart(ChannelId);

        // Assert
        Assert.True(started);
    }

    [Fact]
    public void TryStart_FirstCallForChannel_ReturnsTrue()
    {
        // Act
        var started = _cooldown.TryStart(ChannelId);

        // Assert
        Assert.True(started);
    }

    [Fact]
    public void TryStart_WithinCooldown_ReturnsFalse()
    {
        // Arrange
        _cooldown.TryStart(ChannelId);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Start.AddMinutes(4));

        // Act
        var started = _cooldown.TryStart(ChannelId);

        // Assert
        Assert.False(started);
    }

    [Fact]
    public void TryStart_AfterCooldownElapsed_ReturnsTrue()
    {
        // Arrange
        _cooldown.TryStart(ChannelId);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Start.AddMinutes(5));

        // Act
        var started = _cooldown.TryStart(ChannelId);

        // Assert
        Assert.True(started);
    }

    [Fact]
    public void TryStart_RejectedCall_DoesNotExtendCooldown()
    {
        // Arrange
        _cooldown.TryStart(ChannelId);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Start.AddMinutes(4));
        _cooldown.TryStart(ChannelId);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(Start.AddMinutes(5));

        // Act
        var started = _cooldown.TryStart(ChannelId);

        // Assert
        Assert.True(started);
    }

    [Fact]
    public void TryStart_OtherChannelOnCooldown_ReturnsTrue()
    {
        // Arrange
        _cooldown.TryStart(ChannelId);

        // Act
        var started = _cooldown.TryStart(2);

        // Assert
        Assert.True(started);
    }
}
