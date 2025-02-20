namespace BranikBot.Infrastructure.Services.Abstractions;

public interface IWebScrapingService
{
    Task<decimal> GetMarketPriceAsync();
}
