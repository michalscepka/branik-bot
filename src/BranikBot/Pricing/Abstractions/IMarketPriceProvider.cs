namespace BranikBot.Pricing.Abstractions;

public interface IMarketPriceProvider
{
    ValueTask<decimal?> GetPriceAsync();
}
