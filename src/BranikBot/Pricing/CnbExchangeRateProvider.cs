using System.Globalization;
using System.Xml.Linq;
using BranikBot.Configuration;
using BranikBot.Pricing.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Pricing;

public class CnbExchangeRateProvider(
    HttpClient httpClient,
    IOptions<ExchangeRateConfiguration> configuration,
    ILogger<CnbExchangeRateProvider> logger) : IExchangeRateProvider
{
    private readonly ExchangeRateConfiguration _configuration = configuration.Value;

    public async ValueTask<decimal?> GetExchangeRateAsync(Currency currency)
    {
        if (currency is Currency.Czk)
            return 1m;

        return await FetchExchangeRateAsync();
    }

    private async Task<decimal?> FetchExchangeRateAsync()
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

        return null;
    }
}
