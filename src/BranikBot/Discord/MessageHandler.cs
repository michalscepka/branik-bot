using BranikBot.Persistence;
using BranikBot.Replies;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Discord;

public class MessageHandler(
    GatewayClient gatewayClient,
    BotMessageReader botMessageReader,
    TrustedBotStore trustedBotStore,
    ReplyBuilder replyBuilder,
    ChannelCooldown channelCooldown,
    ILogger<MessageHandler> logger)
{
    public async ValueTask ProcessIncomingMessage(Message message)
    {
        // Check if message is from Branik Bot
        if (message.Author.Id == gatewayClient.Id)
            return;

        if (channelCooldown.IsActive(message.ChannelId))
            return;

        try
        {
            var isBot = message.Author.IsBot;
            if (isBot && !IsTrustedBot(message))
                return;

            var content = isBot
                ? await botMessageReader.ReadAsync(message)
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

    private bool IsTrustedBot(Message message) =>
        message.GuildId is { } guildId && trustedBotStore.IsTrusted(guildId, message.Author.Id);
}
