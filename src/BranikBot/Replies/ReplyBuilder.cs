using BranikBot.Configuration;
using BranikBot.Parsing;
using BranikBot.Persistence;
using BranikBot.Pricing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Replies;

public class ReplyBuilder(
    PriceHistoryStore priceHistoryStore,
    LatestExchangeRate latestExchangeRate,
    IOptions<DiscordConfiguration> discordConfiguration,
    ILogger<ReplyBuilder> logger)
{
    private readonly int _maxAmounts = discordConfiguration.Value.MaxAmountsPerReply;

    public string? Build(string content)
    {
        var amounts = content.ExtractAmounts().ToList();
        if (amounts.Count is 0)
            return null;

        var marketPrice = priceHistoryStore.GetLatest()?.Price;
        if (marketPrice is null)
        {
            logger.LogWarning("No market price stored yet, skipping response.");
            return null;
        }

        var exchangeRates = GetExchangeRates(amounts);

        var reply = MessageFormatter.FormatMessage(amounts, marketPrice.Value, exchangeRates, _maxAmounts);
        return reply.Length is 0 ? null : reply;
    }

    private Dictionary<Currency, decimal> GetExchangeRates(List<UserInput> amounts)
    {
        var exchangeRates = new Dictionary<Currency, decimal>();

        foreach (var currency in amounts.Select(amount => amount.Currency).Distinct())
        {
            if (currency is Currency.Czk)
                continue;

            var rate = latestExchangeRate.Get(currency);
            if (rate is null)
            {
                logger.LogWarning("No {Currency} exchange rate sampled yet, skipping those amounts.", currency);
                continue;
            }

            exchangeRates[currency] = rate.Value;
        }

        return exchangeRates;
    }
}
