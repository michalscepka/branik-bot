using BranikBot.Infrastructure.Configuration;
using BranikBot.Application.Services;
using BranikBot.Infrastructure.Helpers;
using BranikBot.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public class MessageHandler(
    GatewayClient gatewayClient,
    IPriceService priceService,
    IMessageFormatter messageFormatter,
    IOptions<DiscordConfiguration> discordConfiguration,
    ILogger<MessageHandler> logger) : IMessageHandler
{
    private readonly DiscordConfiguration _discordConfiguration = discordConfiguration.Value;
    private readonly Dictionary<ulong, DateTime> _lastMessageTimestamps = [];
    private readonly Lock _cooldownLock = new();

    public async ValueTask ProcessIncomingMessage(Message message)
    {
        if (message.Author.IsBot)
            return;

        try
        {
            logger.LogInformation("[{ChannelId}] {Username}: {Content}", message.ChannelId, message.Author.Username,
                message.Content);

            var amounts = message.Content.ExtractAmounts().ToList();
            if (amounts.Count is 0)
                return;

            if (IsChannelOnCooldown(message.ChannelId))
            {
                logger.LogInformation("Cooldown active for channel {ChannelId}, skipping response.", message.ChannelId);
                return;
            }

            var marketPrice = await priceService.GetPriceAsync();
            var chatMessage = await messageFormatter.FormatMessageAsync(amounts, marketPrice);

            await gatewayClient.Rest.SendMessageAsync(message.ChannelId, chatMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message in channel {ChannelId}", message.ChannelId);
        }
    }

    private bool IsChannelOnCooldown(ulong channelId)
    {
        var utcNow = DateTime.UtcNow;
        var cooldown = _discordConfiguration.ChannelCooldown;

        lock (_cooldownLock)
        {
            if (_lastMessageTimestamps.TryGetValue(channelId, out var lastSent))
            {
                if (utcNow - lastSent < cooldown)
                    return true;
            }

            _lastMessageTimestamps[channelId] = utcNow;
            return false;
        }
    }
}
