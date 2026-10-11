using BranikBot.Persistence;
using BranikBot.Pricing.Abstractions;
using BranikBot.Replies;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;

namespace BranikBot.Discord;

// Shows the current price as the custom status under the bot's name.
// Discord forgets the status when the session is re-created, so it is sent on every Ready
// and again after every stored price sample.
public class BotStatus(
    GatewayClient gatewayClient,
    PriceHistoryStore priceHistoryStore,
    ILogger<BotStatus> logger) : IPriceSampleListener
{
    // Discord requires a name for every activity but shows only the state of a custom one.
    private const string ActivityName = "Custom Status";

    public Task PriceSampledAsync() => RefreshAsync();

    public async Task RefreshAsync()
    {
        try
        {
            var latest = priceHistoryStore.GetLatest();
            if (latest is null)
                return;

            var text = string.Format(Messages.Status, latest.Price);
            var presence = new PresenceProperties(UserStatusType.Online)
            {
                Activities = [new UserActivityProperties(ActivityName, UserActivityType.Custom) { State = text }]
            };

            await gatewayClient.UpdatePresenceAsync(presence);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not update the bot status");
        }
    }
}
