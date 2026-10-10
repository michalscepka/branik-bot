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
    private static readonly CultureInfo FeedCulture = new("cs-CZ");

    private readonly ExchangeRateConfiguration _configuration = configuration.Value;

    public async ValueTask<IReadOnlyDictionary<Currency, decimal>?> GetExchangeRatesAsync()
    {
        try
        {
            var response = await httpClient.GetStringAsync(_configuration.Url);
            var doc = XDocument.Parse(response);
            var rates = new Dictionary<Currency, decimal>();

            foreach (var row in doc.Descendants("radek"))
            {
                var currency = TryGetCurrency(row.Attribute("kod")?.Value);
                var rate = TryGetRate(row.Attribute("kurz")?.Value, row.Attribute("mnozstvi")?.Value);

                if (currency is not null && rate is not null)
                    rates[currency.Value] = rate.Value;
            }

            return rates;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to fetch exchange rates from {Url}", _configuration.Url);
            return null;
        }
    }

    private static Currency? TryGetCurrency(string? code)
    {
        foreach (var currency in Enum.GetValues<Currency>())
        {
            if (currency is not Currency.Czk && currency.ToString().Equals(code, StringComparison.OrdinalIgnoreCase))
                return currency;
        }

        return null;
    }

    private static decimal? TryGetRate(string? rateText, string? amountText)
    {
        if (decimal.TryParse(rateText, NumberStyles.Number, FeedCulture, out var rate) &&
            decimal.TryParse(amountText, NumberStyles.Number, FeedCulture, out var amount) &&
            amount > 0)
        {
            return rate / amount;
        }

        return null;
    }
}
