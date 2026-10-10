using System.Text;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using Polly;
using Polly.Retry;

namespace BranikBot.Discord;

// A bot can post a placeholder first and fill it in later, so the gateway payload may not be final.
// The message is re-fetched over REST until it is no longer marked as loading.
public class BotMessageReader(GatewayClient gatewayClient, ILogger<BotMessageReader> logger)
{
    private const int RetryAttempts = 30;
    private const int RetryDelay = 1;

    private readonly ResiliencePipeline<RestMessage> _pipeline = new ResiliencePipelineBuilder<RestMessage>()
        .AddRetry(new RetryStrategyOptions<RestMessage>
        {
            ShouldHandle = new PredicateBuilder<RestMessage>()
                .HandleResult(m => m.Flags.HasFlag(MessageFlags.Loading)),
            BackoffType = DelayBackoffType.Constant,
            Delay = TimeSpan.FromSeconds(RetryDelay),
            MaxRetryAttempts = RetryAttempts,
            OnRetry = args =>
            {
                logger.LogInformation("Bot message still loading (attempt {Attempt} of {RetryAttempts}).",
                    args.AttemptNumber + 1, RetryAttempts);

                return default;
            }
        })
        .Build();

    public async ValueTask<string> ReadAsync(Message message)
    {
        var restMessage = await _pipeline.ExecuteAsync(async ct =>
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
}
