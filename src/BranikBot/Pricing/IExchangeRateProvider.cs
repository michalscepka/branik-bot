namespace BranikBot.Pricing;

public interface IExchangeRateProvider
{
    ValueTask<decimal?> GetExchangeRateAsync(Currency currency);
}
