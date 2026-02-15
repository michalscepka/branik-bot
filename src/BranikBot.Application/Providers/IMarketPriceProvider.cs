namespace BranikBot.Application.Providers;

public interface IMarketPriceProvider
{
    ValueTask<decimal> GetPriceAsync();
}
