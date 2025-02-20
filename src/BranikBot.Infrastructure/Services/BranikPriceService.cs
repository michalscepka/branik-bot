using System.Globalization;
using BranikBot.Infrastructure.Configuration;
using BranikBot.Infrastructure.Services.Abstractions;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Infrastructure.Services;

public class BranikPriceService : IWebScrapingService
{
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BranikPriceService> _logger;
    private readonly CashingConfiguration _cashingConfig;
    
    private const string CacheKey = "BranikPrice";
    private const decimal DefaultMarketPrice = 45m;

    public BranikPriceService(IMemoryCache cache, IHttpClientFactory httpClientFactory,
        ILogger<BranikPriceService> logger, IOptions<CashingConfiguration> cashingConfiguration)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cashingConfig = cashingConfiguration.Value;
    }
    
    public async Task<decimal> GetMarketPriceAsync()
    {
        if (_cache.TryGetValue(CacheKey, out decimal cachedPrice))
        {
            _logger.LogInformation("Returning cached price: {Price}", cachedPrice);
            return cachedPrice;
        }

        var price = await FetchBranikPriceAsync();

        if (!price.HasValue)
            return DefaultMarketPrice;
        
        _cache.Set(CacheKey, price.Value, _cashingConfig.DurationMinutes);
        _logger.LogInformation("Fetched and cached new price: {Price}", price.Value);
        return price.Value;
    }
    
    private async Task<decimal?> FetchBranikPriceAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync(_cashingConfig.MarketPriceUrl);
            
            var doc = new HtmlDocument();
            doc.LoadHtml(response);
            
            var lowPriceNode = doc.DocumentNode.SelectSingleNode("//span[@itemprop='lowPrice']");
            var priceString = lowPriceNode.GetAttributeValue("content", "N/A");
            
            _logger.LogInformation("Price: {price} CZK", priceString);
            
            return decimal.Parse(priceString, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch price from {Url}", _cashingConfig.MarketPriceUrl);
            return null;
        }
    }
}
