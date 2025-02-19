namespace BranikBot.Infrastructure.Services;

public interface IWebScrapingService
{
    Task<decimal> GetMarketPriceAsync();
}
