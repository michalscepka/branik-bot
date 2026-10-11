using BranikBot.Configuration;
using BranikBot.Persistence;
using BranikBot.Pricing.Abstractions;
using BranikBot.Replies;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;

namespace BranikBot.Discord;

// Shows the current price and the bot's version as the custom status under its name.
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
            var text = StatusText.Create(priceHistoryStore.GetLatest()?.Price, AppVersion.Current);
            var presence = new PresenceProperties(UserStatusType.Online)
            {
                Activities = [new UserActivityProperties(ActivityName, UserActivityType.Custom) { State = text }]
            };

            await gatewayClient.UpdatePresenceAsync(presence);
        }
        catch (Exception ex)
        {
            // Expected while the gateway is not connected. The next Ready or price sample sends it again.
            logger.LogDebug(ex, "Could not update the bot status");
        }
    }
}
