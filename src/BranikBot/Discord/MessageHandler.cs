using System.Net;
using BranikBot.Persistence;
using BranikBot.Replies;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Rest;

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
    // Put on a message that had an amount but lost the reply chance, to show the bot noticed it.
    private static readonly ReactionEmojiProperties SkippedReaction = new("🍺");

    public async ValueTask ProcessIncomingMessage(Message message)
    {
        // Check if message is from Branik Bot
        if (message.Author.Id == gatewayClient.Id)
            return;

        if (message.GuildId is null)
            return;

        var guildId = message.GuildId.Value;

        var isMentioned = message.MentionedUsers.Any(user => user.Id == gatewayClient.Id);

        if (!isMentioned && replyCooldown.IsActive(guildId))
        {
            logger.LogDebug("Cooldown active for guild {GuildId}, ignoring message {MessageId}.", guildId, message.Id);
            return;
        }

        try
        {
            var reply = await BuildReplyAsync(message, guildId);

            if (reply is null && isMentioned && message.ReferencedMessage is not null)
                reply = await BuildReplyAsync(message.ReferencedMessage, guildId);

            if (reply is null)
            {
                logger.LogDebug("Nothing to reply to in message {MessageId} in guild {GuildId}.", message.Id,
                    guildId);
                return;
            }

            if (!isMentioned && !replyChance.Roll())
            {
                logger.LogInformation("Reply chance skipped message {MessageId} in guild {GuildId}.", message.Id, guildId);
                await AddSkippedReactionAsync(message);
                return;
            }

            if (!isMentioned && !replyCooldown.TryStart(guildId))
            {
                logger.LogInformation("Cooldown active for guild {GuildId}, skipping reply to message {MessageId}.", guildId, message.Id);
                return;
            }

            logger.LogInformation(
                "Replying to message {MessageId} in channel {ChannelId} of guild {GuildId} (mentioned: {IsMentioned}).",
                message.Id, message.ChannelId, guildId, isMentioned);

            await SendReplyAsync(message, reply);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message in channel {ChannelId}", message.ChannelId);
        }
    }

    private async ValueTask<string?> BuildReplyAsync(RestMessage message, ulong guildId)
    {
        if (message.Author.Id == gatewayClient.Id)
            return null;

        var isBot = message.Author.IsBot;
        if (isBot && !trustedBotStore.IsTrusted(guildId, message.Author.Id))
        {
            logger.LogDebug("Ignoring message {MessageId} from untrusted bot {BotId} in guild {GuildId}.",
                message.Id, message.Author.Id, guildId);
            return null;
        }

        var content = isBot
            ? await botMessageReader.ReadAsync(message)
            : message.Content;

        return replyBuilder.Build(content);
    }

    private async ValueTask AddSkippedReactionAsync(Message message)
    {
        try
        {
            await gatewayClient.Rest.AddMessageReactionAsync(message.ChannelId, message.Id, SkippedReaction);
        }
        catch (RestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
        {
            // Reacting needs the Add Reactions permission. A server that withholds it simply gets no reactions,
            // so this is not worth a warning on every skipped message.
            logger.LogDebug("Not allowed to react to message {MessageId} in channel {ChannelId}.", message.Id, message.ChannelId);
        }
    }

    private async ValueTask SendReplyAsync(Message message, string reply)
    {
        var properties = new MessageProperties
        {
            Content = reply,
            AllowedMentions = AllowedMentionsProperties.None,
            MessageReference = MessageReferenceProperties.Reply(message.Id, failIfNotExists: false)
        };

        try
        {
            await gatewayClient.Rest.SendMessageAsync(message.ChannelId, properties);
        }
        catch (RestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
        {
            // Replying needs the Read Message History permission. Without it the answer is sent on its own.
            logger.LogWarning("Not allowed to reply to message {MessageId} in channel {ChannelId}, sending a plain message.", message.Id, message.ChannelId);

            properties.MessageReference = null;
            await gatewayClient.Rest.SendMessageAsync(message.ChannelId, properties);
        }
    }
}
