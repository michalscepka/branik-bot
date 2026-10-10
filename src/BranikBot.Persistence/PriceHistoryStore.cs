using BranikBot.Pricing;
using Microsoft.Data.Sqlite;

namespace BranikBot.Persistence;

public class PriceHistoryStore(SqliteDatabase database)
{
    public PricePeriod? GetLatest()
    {
        using var connection = database.OpenConnection();
        return ReadLatest(connection)?.Period;
    }

    // One row per price period: an unchanged price only moves last_seen_at forward.
    public void Record(decimal price, DateTimeOffset seenAt)
    {
        using var connection = database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        var latest = ReadLatest(connection);
        var seenAtUtc = seenAt.ToUniversalTime();

        using var command = connection.CreateCommand();
        if (latest is { } row && row.Period.Price == price)
        {
            command.CommandText = "UPDATE price_history SET last_seen_at = $seenAt WHERE id = $id";
            command.Parameters.AddWithValue("$id", row.Id);
        }
        else
        {
            command.CommandText =
                "INSERT INTO price_history (price, first_seen_at, last_seen_at) VALUES ($price, $seenAt, $seenAt)";
            command.Parameters.AddWithValue("$price", price);
        }

        command.Parameters.AddWithValue("$seenAt", seenAtUtc);
        command.ExecuteNonQuery();

        transaction.Commit();
    }

    private static (long Id, PricePeriod Period)? ReadLatest(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, price, first_seen_at, last_seen_at FROM price_history ORDER BY id DESC LIMIT 1";

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        var period = new PricePeriod(reader.GetDecimal(1), reader.GetDateTimeOffset(2), reader.GetDateTimeOffset(3));
        return (reader.GetInt64(0), period);
    }
}
