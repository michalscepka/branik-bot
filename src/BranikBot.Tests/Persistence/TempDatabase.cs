using BranikBot.Persistence;
using Microsoft.Data.Sqlite;

namespace BranikBot.Tests.Persistence;

public sealed class TempDatabase : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"branik-tests-{Guid.NewGuid():N}");

    public SqliteDatabase Create() => new(Path.Combine(_directory, "branik.db"));

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }
}
