namespace BranikBot.Pricing.Abstractions;

public interface IExchangeRateProvider
{
    ValueTask<decimal?> GetExchangeRateAsync(Currency currency);
}
