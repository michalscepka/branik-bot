using BranikBot.Infrastructure.Configuration;
using BranikBot.Domain.Services;
using BranikBot.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public class MessageHandler : IMessageHandler
{
    private readonly GatewayClient _gatewayClient;
    private readonly IPriceService _priceService;
    private readonly IMessageFormatter _messageFormatter;
    private readonly DiscordConfiguration _discordConfiguration;
    private readonly ILogger<MessageHandler> _logger;
    private readonly Dictionary<ulong, DateTime> _lastMessageTimestamps;
    private readonly Lock _cooldownLock = new();

    public MessageHandler(
        GatewayClient gatewayClient,
        IPriceService priceService,
        IMessageFormatter messageFormatter,
        IOptions<DiscordConfiguration> discordConfiguration,
        ILogger<MessageHandler> logger)
    {
        _gatewayClient = gatewayClient;
        _priceService = priceService;
        _messageFormatter = messageFormatter;
        _discordConfiguration = discordConfiguration.Value;
        _logger = logger;
        _lastMessageTimestamps = new Dictionary<ulong, DateTime>();
    }

    public async ValueTask ProcessIncomingMessage(Message message)
    {
        if (message.Author.IsBot)
            return;

        try
        {
            _logger.LogInformation("[{ChannelId}] {Username}: {Content}", message.ChannelId, message.Author.Username,
                message.Content);

            var prices = message.Content.ExtractPrices().ToList();
            if (prices.Count is 0)
                return;

            if (IsChannelOnCooldown(message.ChannelId))
            {
                _logger.LogInformation("Cooldown active for channel {ChannelId}, skipping response.", message.ChannelId);
                return;
            }

            var marketPrice = await _priceService.GetPriceAsync();
            var chatMessage = await _messageFormatter.FormatMessageAsync(prices, marketPrice);

            await _gatewayClient.Rest.SendMessageAsync(message.ChannelId, chatMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message in channel {ChannelId}", message.ChannelId);
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
