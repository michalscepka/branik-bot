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

    // Returns null when the text holds no amounts or there is nothing to answer with.
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

        var euroExchangeRate = latestExchangeRate.Get(Currency.Eur);
        if (euroExchangeRate is null && amounts.Any(a => a.Currency is Currency.Eur))
            logger.LogWarning("No EUR exchange rate sampled yet, skipping EUR amounts.");

        var reply = MessageFormatter.FormatMessage(amounts, marketPrice.Value, euroExchangeRate, _maxAmounts);
        return reply.Length is 0 ? null : reply;
    }
}
