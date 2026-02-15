using System.Globalization;
using System.Xml.Linq;
using BranikBot.Application.Providers;
using BranikBot.Domain.Enums;
using BranikBot.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Infrastructure.Providers;

public class CnbExchangeRateProvider(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<ExchangeRateConfiguration> configuration,
    ILogger<CnbExchangeRateProvider> logger) : IExchangeRateProvider
{
    private readonly ExchangeRateConfiguration _configuration = configuration.Value;

    private const string CacheKeyPrefix = "ExchangeRate_";

    public async ValueTask<decimal> GetExchangeRateAsync(Currency currency)
    {
        if (currency is Currency.Czk)
            return 1m;

        var cacheKey = $"{CacheKeyPrefix}{currency}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _configuration.CacheDuration;

            return await FetchExchangeRateAsync();
        });
    }

    private async Task<decimal> FetchExchangeRateAsync()
    {
        try
        {
            var response = await httpClient.GetStringAsync(_configuration.Url);
            var doc = XDocument.Parse(response);

            var rows = doc.Descendants("radek");
            foreach (var row in rows)
            {
                var code = row.Attribute("kod")?.Value;
                var rateStr = row.Attribute("kurz")?.Value;
                var amountStr = row.Attribute("mnozstvi")?.Value;

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(rateStr) || string.IsNullOrEmpty(amountStr))
                    continue;

                if (!code.Equals(nameof(Currency.Eur), StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (decimal.TryParse(rateStr, NumberStyles.Number, new CultureInfo("cs-CZ"), out var rate) &&
                    decimal.TryParse(amountStr, out var amount))
                {
                    return rate / amount;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to fetch exchange rate from {Url}", _configuration.Url);
        }

        return 0m;
    }
}
