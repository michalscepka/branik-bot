namespace BranikBot.Pricing;

public interface IMarketPriceProvider
{
    ValueTask<decimal?> GetPriceAsync();
}
