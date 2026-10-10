using BranikBot.Configuration;
using BranikBot.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace BranikBot.Tests.Pricing;

public sealed class TempDatabase : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"branik-tests-{Guid.NewGuid():N}");

    public SqliteDatabase Create()
    {
        var options = Options.Create(new DatabaseConfiguration { Path = Path.Combine(_directory, "branik.db") });
        return new SqliteDatabase(options);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }
}
