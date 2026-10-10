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
    ReplyCooldown replyCooldown,
    ReplyChance replyChance,
    ILogger<MessageHandler> logger)
{
    public async ValueTask ProcessIncomingMessage(Message message)
    {
        // Check if message is from Branik Bot
        if (message.Author.Id == gatewayClient.Id)
            return;

        if (message.GuildId is null)
            return;

        var guildId = message.GuildId.Value;
        if (replyCooldown.IsActive(guildId))
        {
            logger.LogDebug("Cooldown active for guild {GuildId}, ignoring message {MessageId}.", guildId, message.Id);
            return;
        }

        try
        {
            // Bots are ignored unless an admin of this guild trusted them.
            var isBot = message.Author.IsBot;
            if (isBot && !trustedBotStore.IsTrusted(guildId, message.Author.Id))
            {
                logger.LogDebug("Ignoring message {MessageId} from untrusted bot {BotId} in guild {GuildId}.", message.Id, message.Author.Id, guildId);
                return;
            }

            var content = isBot
                ? await botMessageReader.ReadAsync(message)
                : message.Content;

            var reply = replyBuilder.Build(content);
            if (reply is null)
            {
                logger.LogDebug("Nothing to reply to in message {MessageId} in guild {GuildId}.", message.Id, guildId);
                return;
            }

            if (!replyChance.Roll())
            {
                logger.LogInformation("Reply chance skipped message {MessageId} in guild {GuildId}.", message.Id, guildId);
                return;
            }

            if (!replyCooldown.TryStart(guildId))
            {
                logger.LogInformation("Cooldown active for guild {GuildId}, skipping reply to message {MessageId}.", guildId, message.Id);
                return;
            }

            logger.LogInformation("Replying to message {MessageId} in channel {ChannelId} of guild {GuildId}.", message.Id, message.ChannelId, guildId);

            await gatewayClient.Rest.SendMessageAsync(message.ChannelId, reply);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message in channel {ChannelId}", message.ChannelId);
        }
    }
}
