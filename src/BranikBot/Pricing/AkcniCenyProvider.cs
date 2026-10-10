using System.Globalization;
using BranikBot.Configuration;
using BranikBot.Pricing.Abstractions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Pricing;

public class AkcniCenyProvider(
    HttpClient httpClient,
    ILogger<AkcniCenyProvider> logger,
    IOptions<MarketConfiguration> configuration) : IMarketPriceProvider
{
    private readonly MarketConfiguration _configuration = configuration.Value;

    public async ValueTask<decimal?> GetPriceAsync()
    {
        try
        {
            var response = await httpClient.GetStringAsync(_configuration.Url);

            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var lowPriceNode = doc.DocumentNode.SelectSingleNode("//span[@itemprop='lowPrice']");
            var priceString = lowPriceNode.GetAttributeValue("content", "N/A");
            var price = decimal.Parse(priceString, CultureInfo.InvariantCulture);

            if (price <= 0)
            {
                logger.LogError("Ignoring non-positive price {Price} from {Url}", price, _configuration.Url);
                return null;
            }

            logger.LogInformation("Fetched price: {Price} CZK", price);
            return price;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch price from {Url}", _configuration.Url);
            return null;
        }
    }
}
