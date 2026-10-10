namespace BranikBot.Pricing.Abstractions;

public interface IExchangeRateProvider
{
    ValueTask<IReadOnlyDictionary<Currency, decimal>?> GetExchangeRatesAsync();
}
