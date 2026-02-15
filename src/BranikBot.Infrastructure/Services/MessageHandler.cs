using System.Text;
using BranikBot.Application.Formatting;
using BranikBot.Application.Providers;
using BranikBot.Infrastructure.Configuration;
using BranikBot.Infrastructure.Helpers;
using BranikBot.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Rest;
using Polly;
using Polly.Retry;

namespace BranikBot.Infrastructure.Services;

public class MessageHandler(
    GatewayClient gatewayClient,
    IMarketPriceProvider marketPriceClient,
    IMessageFormatter messageFormatter,
    IOptions<DiscordConfiguration> discordConfiguration,
    ILogger<MessageHandler> logger) : IMessageHandler
{
    private readonly DiscordConfiguration _discordConfig = discordConfiguration.Value;
    private readonly Dictionary<ulong, DateTime> _lastMessageTimestamps = [];
    private readonly Lock _cooldownLock = new();
    private const int RetryAttempts = 30;
    private const int RetryDelay = 1;

    private readonly ResiliencePipeline<RestMessage> _botMessagePipeline = new ResiliencePipelineBuilder<RestMessage>()
        .AddRetry(new RetryStrategyOptions<RestMessage>
        {
            ShouldHandle = new PredicateBuilder<RestMessage>()
                .HandleResult(m => m.Flags != 0),
            BackoffType = DelayBackoffType.Constant,
            Delay = TimeSpan.FromSeconds(RetryDelay),
            MaxRetryAttempts = RetryAttempts,
            OnRetry = args =>
            {
                logger.LogInformation("Retry for Euro Champion (Attempt {Attempt} of {RetryAttempts}).",
                    args.AttemptNumber + 1, RetryAttempts);

                if (args.AttemptNumber >= RetryAttempts)
                    logger.LogWarning("Max retries reached for Euro Champion message. Moving on with current content.");

                return default;
            }
        })
        .Build();

    public async ValueTask ProcessIncomingMessage(Message message)
    {
        // Check if message is from Branik Bot
        if (message.Author.Id == gatewayClient.Id)
            return;

        if (message.Author.IsBot && message.Author.Id != _discordConfig.EuroChampionId)
            return;

        try
        {
            var content = message.Content;

            if (message.Author.Id == _discordConfig.EuroChampionId)
                content = await GetEuroChampionMessage(message);

            logger.LogInformation("[{ChannelId}] {Username}: {Content}", message.ChannelId, message.Author.Username,
                content);

            var amounts = content.ExtractAmounts().ToList();
            if (amounts.Count is 0)
                return;

            if (IsChannelOnCooldown(message.ChannelId))
            {
                logger.LogInformation("Cooldown active for channel {ChannelId}, skipping response.", message.ChannelId);
                return;
            }

            var marketPrice = await marketPriceClient.GetPriceAsync();
            var chatMessage = await messageFormatter.FormatMessageAsync(amounts, marketPrice);

            await gatewayClient.Rest.SendMessageAsync(message.ChannelId, chatMessage);

            SetChannelCooldown(message.ChannelId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message in channel {ChannelId}", message.ChannelId);
        }
    }

    private async ValueTask<string> GetEuroChampionMessage(Message message)
    {
        var restMessage = await _botMessagePipeline.ExecuteAsync(async ct =>
            await gatewayClient.Rest.GetMessageAsync(message.ChannelId, message.Id, cancellationToken: ct));

        var sb = new StringBuilder(restMessage.Content);
        foreach (var embed in restMessage.Embeds)
        {
            sb.AppendLine(embed.Title);
            sb.AppendLine(embed.Description);
            foreach (var field in embed.Fields)
            {
                sb.AppendLine(field.Name);
                sb.AppendLine(field.Value);
            }

            if (embed.Footer != null)
                sb.AppendLine(embed.Footer.Text);
        }

        return sb.ToString();
    }

    private bool IsChannelOnCooldown(ulong channelId)
    {
        var utcNow = DateTime.UtcNow;
        var cooldown = _discordConfig.ChannelCooldown;

        lock (_cooldownLock)
        {
            if (!_lastMessageTimestamps.TryGetValue(channelId, out var lastSent))
                return false;

            return utcNow - lastSent < cooldown;
        }
    }

    private void SetChannelCooldown(ulong channelId)
    {
        var utcNow = DateTime.UtcNow;

        lock (_cooldownLock)
        {
            _lastMessageTimestamps[channelId] = utcNow;
        }
    }
}
