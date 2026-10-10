using Microsoft.Data.Sqlite;

namespace BranikBot.Persistence;

public class SqliteDatabase(string path)
{
    private const string Schema =
        """
        CREATE TABLE IF NOT EXISTS price_history (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            price TEXT NOT NULL,
            first_seen_at TEXT NOT NULL,
            last_seen_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS trusted_bot (
            guild_id INTEGER NOT NULL,
            bot_id INTEGER NOT NULL,
            added_at TEXT NOT NULL,
            PRIMARY KEY (guild_id, bot_id)
        );
        """;

    private readonly string _connectionString = InitializeDatabase(path);

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static string InitializeDatabase(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (Path.GetDirectoryName(fullPath) is { } directory)
            Directory.CreateDirectory(directory);

        var connectionString = new SqliteConnectionStringBuilder { DataSource = fullPath }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = Schema;
        command.ExecuteNonQuery();

        return connectionString;
    }
}
