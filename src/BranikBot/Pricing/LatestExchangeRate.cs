using System.Collections.Concurrent;

namespace BranikBot.Pricing;

// Holds the last successfully sampled rate per currency in memory.
public class LatestExchangeRate
{
    private readonly ConcurrentDictionary<Currency, decimal> _rates = new();

    public decimal? Get(Currency currency) => _rates.TryGetValue(currency, out var rate) ? rate : null;

    public void Set(Currency currency, decimal rate) => _rates[currency] = rate;
}
