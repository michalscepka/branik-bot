using System.Globalization;
using System.Xml.Linq;
using BranikBot.Infrastructure.Configuration;
using BranikBot.Infrastructure.Enums;
using BranikBot.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Infrastructure.Services;

public class CnbExchangeRateService : IExchangeRateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CnbExchangeRateService> _logger;
    private readonly ExchangeRateConfiguration _configuration;

    private const string CacheKeyPrefix = "ExchangeRate_";

    public CnbExchangeRateService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<ExchangeRateConfiguration> configuration,
        ILogger<CnbExchangeRateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
        _configuration = configuration.Value;
    }

    public async Task<decimal> GetExchangeRateAsync(Currency currency)
    {
        if (currency is Currency.Czk)
            return 1m;

        var cacheKey = $"{CacheKeyPrefix}{currency}";

        if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
            return cachedRate;

        try
        {
            var rates = await FetchExchangeRatesAsync();

            if (rates.TryGetValue(currency, out var rate))
            {
                _cache.Set(cacheKey, rate, _configuration.CacheDuration);
                return rate;
            }

            _logger.LogWarning("Exchange rate for {Currency} not found in CNB data.", currency);
            throw new InvalidOperationException($"Exchange rate for {currency} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch exchange rate for {Currency}.", currency);
            throw;
        }
    }

    private async Task<Dictionary<Currency, decimal>> FetchExchangeRatesAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetStringAsync(_configuration.Url);
        var doc = XDocument.Parse(response);

        var result = new Dictionary<Currency, decimal>();

        var rows = doc.Descendants("radek");
        foreach (var row in rows)
        {
            var code = row.Attribute("kod")?.Value;
            var rateStr = row.Attribute("kurz")?.Value;
            var amountStr = row.Attribute("mnozstvi")?.Value;

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(rateStr) || string.IsNullOrEmpty(amountStr))
                continue;

            if (!Enum.TryParse(code, true, out Currency currency))
                continue;

            if (decimal.TryParse(rateStr, NumberStyles.Number, new CultureInfo("cs-CZ"), out var rate) &&
                decimal.TryParse(amountStr, out var amount))
            {
                result[currency] = rate / amount;
            }
        }

        return result;
    }
}
