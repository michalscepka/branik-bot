using BranikBot.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace BranikBot.Storage;

public class SqliteDatabase(IOptions<DatabaseConfiguration> configuration)
{
    private const string Schema =
        """
        CREATE TABLE IF NOT EXISTS price_history (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            price TEXT NOT NULL,
            first_seen_at TEXT NOT NULL,
            last_seen_at TEXT NOT NULL
        );
        """;

    private readonly string _connectionString = InitializeDatabase(configuration.Value.Path);

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
