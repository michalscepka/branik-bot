using BranikBot.Persistence;

namespace BranikBot.Tests.Persistence;

public sealed class TrustedBotStoreTests : IDisposable
{
    private const ulong GuildId = 100;
    private const ulong OtherGuildId = 200;
    private const ulong BotId = 1413208879197847572;
    private const ulong OtherBotId = 1413208879197847573;

    private static readonly DateTimeOffset AddedAt = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _database = new();
    private readonly TrustedBotStore _store;

    public TrustedBotStoreTests()
    {
        _store = new TrustedBotStore(_database.Create());
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void IsTrusted_EmptyDatabase_ReturnsFalse()
    {
        // Act
        var trusted = _store.IsTrusted(GuildId, BotId);

        // Assert
        Assert.False(trusted);
    }

    [Fact]
    public void Add_NewBot_ReturnsTrueAndTrustsIt()
    {
        // Act
        var added = _store.Add(GuildId, BotId, AddedAt);

        // Assert
        Assert.True(added);
        Assert.True(_store.IsTrusted(GuildId, BotId));
    }

    [Fact]
    public void Add_AlreadyTrustedBot_ReturnsFalseAndKeepsOneEntry()
    {
        // Arrange
        _store.Add(GuildId, BotId, AddedAt);

        // Act
        var added = _store.Add(GuildId, BotId, AddedAt.AddHours(1));

        // Assert
        Assert.False(added);
        Assert.Equal([BotId], _store.List(GuildId));
    }

    [Fact]
    public void IsTrusted_BotTrustedInAnotherGuild_ReturnsFalse()
    {
        // Arrange
        _store.Add(OtherGuildId, BotId, AddedAt);

        // Act
        var trusted = _store.IsTrusted(GuildId, BotId);

        // Assert
        Assert.False(trusted);
    }

    [Fact]
    public void Remove_TrustedBot_ReturnsTrueAndStopsTrustingIt()
    {
        // Arrange
        _store.Add(GuildId, BotId, AddedAt);

        // Act
        var removed = _store.Remove(GuildId, BotId);

        // Assert
        Assert.True(removed);
        Assert.False(_store.IsTrusted(GuildId, BotId));
    }

    [Fact]
    public void Remove_UnknownBot_ReturnsFalse()
    {
        // Act
        var removed = _store.Remove(GuildId, BotId);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void Remove_BotTrustedInTwoGuilds_KeepsTheOtherGuild()
    {
        // Arrange
        _store.Add(GuildId, BotId, AddedAt);
        _store.Add(OtherGuildId, BotId, AddedAt);

        // Act
        _store.Remove(GuildId, BotId);

        // Assert
        Assert.True(_store.IsTrusted(OtherGuildId, BotId));
    }

    [Fact]
    public void List_SeveralBots_ReturnsOnlyThisGuildInOrderAdded()
    {
        // Arrange
        _store.Add(GuildId, OtherBotId, AddedAt);
        _store.Add(GuildId, BotId, AddedAt.AddHours(1));
        _store.Add(OtherGuildId, 300, AddedAt);

        // Act
        var botIds = _store.List(GuildId);

        // Assert
        Assert.Equal([OtherBotId, BotId], botIds);
    }

    [Fact]
    public void IsTrusted_IdAboveSignedRange_RoundTrips()
    {
        // Arrange
        _store.Add(ulong.MaxValue, ulong.MaxValue - 1, AddedAt);

        // Act
        var trusted = _store.IsTrusted(ulong.MaxValue, ulong.MaxValue - 1);

        // Assert
        Assert.True(trusted);
        Assert.Equal([ulong.MaxValue - 1], _store.List(ulong.MaxValue));
    }

    [Fact]
    public void IsTrusted_NewStoreOnSameFile_ReturnsStoredBot()
    {
        // Arrange
        _store.Add(GuildId, BotId, AddedAt);

        // Act
        var trusted = new TrustedBotStore(_database.Create()).IsTrusted(GuildId, BotId);

        // Assert
        Assert.True(trusted);
    }
}
