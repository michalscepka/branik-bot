using System.Globalization;
using BranikBot.Application.Providers;
using BranikBot.Infrastructure.Configuration;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Infrastructure.Providers;

public class AkcniCenyProvider(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<AkcniCenyProvider> logger,
    IOptions<MarketConfiguration> configuration) : IMarketPriceProvider
{
    private readonly MarketConfiguration _configuration = configuration.Value;

    private const string CacheKey = "BranikPrice_";
    private const decimal FallbackMarketPrice = 45m;

    public async ValueTask<decimal> GetPriceAsync()
    {
        return await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _configuration.CacheDuration;

            var price = await FetchBranikPriceAsync();
            var finalPrice = price ?? FallbackMarketPrice;

            logger.LogInformation("Fetched and cached new price: {Price}", finalPrice);
            return finalPrice;
        });
    }

    private async Task<decimal?> FetchBranikPriceAsync()
    {
        try
        {
            var response = await httpClient.GetStringAsync(_configuration.Url);

            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var lowPriceNode = doc.DocumentNode.SelectSingleNode("//span[@itemprop='lowPrice']");
            var priceString = lowPriceNode.GetAttributeValue("content", "N/A");

            logger.LogInformation("Price: {price} CZK", priceString);

            return decimal.Parse(priceString, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch price from {Url}", _configuration.Url);
            return null;
        }
    }
}
