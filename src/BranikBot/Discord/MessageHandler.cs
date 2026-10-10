using BranikBot.Configuration;
using BranikBot.Replies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;

namespace BranikBot.Discord;

public class MessageHandler(
    GatewayClient gatewayClient,
    EuroChampionMessageReader euroChampionMessageReader,
    ReplyBuilder replyBuilder,
    ChannelCooldown channelCooldown,
    IOptions<DiscordConfiguration> discordConfiguration,
    ILogger<MessageHandler> logger)
{
    private readonly ulong _euroChampionId = discordConfiguration.Value.EuroChampionId;

    public async ValueTask ProcessIncomingMessage(Message message)
    {
        // Check if message is from Branik Bot
        if (message.Author.Id == gatewayClient.Id)
            return;

        var isEuroChampion = message.Author.Id == _euroChampionId;
        if (message.Author.IsBot && !isEuroChampion)
            return;

        try
        {
            var content = isEuroChampion
                ? await euroChampionMessageReader.ReadAsync(message)
                : message.Content;

            var reply = replyBuilder.Build(content);
            if (reply is null)
                return;

            if (!channelCooldown.TryStart(message.ChannelId))
            {
                logger.LogInformation("Cooldown active for channel {ChannelId}, skipping response.", message.ChannelId);
                return;
            }

            logger.LogInformation("Replying to message {MessageId} in channel {ChannelId}.", message.Id,
                message.ChannelId);

            await gatewayClient.Rest.SendMessageAsync(message.ChannelId, reply);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message in channel {ChannelId}", message.ChannelId);
        }
    }
}
