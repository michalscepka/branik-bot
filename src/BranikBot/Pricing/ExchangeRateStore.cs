using BranikBot.Storage;

namespace BranikBot.Pricing;

public class ExchangeRateStore(SqliteDatabase database)
{
    public decimal? GetLatest(Currency currency)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT rate FROM exchange_rate WHERE currency = $currency";
        command.Parameters.AddWithValue("$currency", currency.ToString());

        using var reader = command.ExecuteReader();
        return reader.Read() ? reader.GetDecimal(0) : null;
    }

    // One row per currency: a new rate replaces the previous one.
    public void Record(Currency currency, decimal rate, DateTimeOffset updatedAt)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO exchange_rate (currency, rate, updated_at) VALUES ($currency, $rate, $updatedAt)
            ON CONFLICT (currency) DO UPDATE SET rate = excluded.rate, updated_at = excluded.updated_at
            """;
        command.Parameters.AddWithValue("$currency", currency.ToString());
        command.Parameters.AddWithValue("$rate", rate);
        command.Parameters.AddWithValue("$updatedAt", updatedAt.ToUniversalTime());
        command.ExecuteNonQuery();
    }
}
