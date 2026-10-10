using Microsoft.Data.Sqlite;

namespace BranikBot.Persistence;

// The bots whose messages are read, per guild. Every other bot is ignored.
public class TrustedBotStore(SqliteDatabase database)
{
    public bool IsTrusted(ulong guildId, ulong botId)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM trusted_bot WHERE guild_id = $guildId AND bot_id = $botId";
        AddIds(command, guildId, botId);

        return command.ExecuteScalar() is not null;
    }

    // Returns false when the bot was already trusted.
    public bool Add(ulong guildId, ulong botId, DateTimeOffset addedAt)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT OR IGNORE INTO trusted_bot (guild_id, bot_id, added_at) VALUES ($guildId, $botId, $addedAt)";
        AddIds(command, guildId, botId);
        command.Parameters.AddWithValue("$addedAt", addedAt.ToUniversalTime());

        return command.ExecuteNonQuery() > 0;
    }

    // Returns false when the bot was not trusted.
    public bool Remove(ulong guildId, ulong botId)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM trusted_bot WHERE guild_id = $guildId AND bot_id = $botId";
        AddIds(command, guildId, botId);

        return command.ExecuteNonQuery() > 0;
    }

    public IReadOnlyList<ulong> List(ulong guildId)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT bot_id FROM trusted_bot WHERE guild_id = $guildId ORDER BY added_at, bot_id";
        command.Parameters.AddWithValue("$guildId", ToStored(guildId));

        using var reader = command.ExecuteReader();
        var botIds = new List<ulong>();
        while (reader.Read())
            botIds.Add(unchecked((ulong)reader.GetInt64(0)));

        return botIds;
    }

    private static void AddIds(SqliteCommand command, ulong guildId, ulong botId)
    {
        command.Parameters.AddWithValue("$guildId", ToStored(guildId));
        command.Parameters.AddWithValue("$botId", ToStored(botId));
    }

    // SQLite integers are signed 64-bit, Discord IDs are unsigned.
    private static long ToStored(ulong id) => unchecked((long)id);
}
