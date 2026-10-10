using BranikBot.Persistence;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace BranikBot.Discord;

[SlashCommand("branik-config", "Nastavení Braník Bota",
    DefaultGuildUserPermissions = Permissions.ManageGuild,
    Contexts = [InteractionContextType.Guild])]
public class ConfigCommands(
    TrustedBotStore trustedBotStore,
    TimeProvider timeProvider,
    ILogger<ConfigCommands> logger) : ApplicationCommandModule<ApplicationCommandContext>
{
    private const string GuildOnly = "Tento příkaz funguje jen na serveru.";

    [SubSlashCommand("bot-add", "Braník Bot začne číst zprávy vybraného bota")]
    public InteractionMessageProperties AddBot(
        [SlashCommandParameter(Name = "bot", Description = "Bot, jehož zprávy se mají číst")] User bot)
    {
        if (Context.Interaction.GuildId is null)
            return Reply(GuildOnly);

        var guildId = Context.Interaction.GuildId.Value;

        if (!bot.IsBot)
            return Reply($"<@{bot.Id}> není bot. Zprávy lidí čtu vždy.");

        if (bot.Id == Context.Client.Id)
            return Reply("Vlastní zprávy nečtu.");

        if (!trustedBotStore.Add(guildId, bot.Id, timeProvider.GetUtcNow()))
            return Reply($"Zprávy bota <@{bot.Id}> už čtu.");

        logger.LogInformation("User {UserId} added trusted bot {BotId} in guild {GuildId}.",
            Context.User.Id, bot.Id, guildId);

        return Reply($"Hotovo, zprávy bota <@{bot.Id}> teď čtu.");
    }

    [SubSlashCommand("bot-remove", "Braník Bot přestane číst zprávy vybraného bota")]
    public InteractionMessageProperties RemoveBot(
        [SlashCommandParameter(Name = "bot", Description = "Bot, jehož zprávy se mají přestat číst")] User bot)
    {
        if (Context.Interaction.GuildId is null)
            return Reply(GuildOnly);

        var guildId = Context.Interaction.GuildId.Value;

        if (!trustedBotStore.Remove(guildId, bot.Id))
            return Reply($"Zprávy bota <@{bot.Id}> nečtu.");

        logger.LogInformation("User {UserId} removed trusted bot {BotId} in guild {GuildId}.",
            Context.User.Id, bot.Id, guildId);

        return Reply($"Hotovo, zprávy bota <@{bot.Id}> už nečtu.");
    }

    [SubSlashCommand("bot-list", "Vypíše boty, jejichž zprávy Braník Bot čte")]
    public InteractionMessageProperties ListBots()
    {
        if (Context.Interaction.GuildId is null)
            return Reply(GuildOnly);

        var guildId = Context.Interaction.GuildId.Value;

        var botIds = trustedBotStore.List(guildId);
        if (botIds.Count == 0)
            return Reply("Nečtu zprávy žádného bota.");

        return Reply($"Čtu zprávy těchto botů: {string.Join(", ", botIds.Select(id => $"<@{id}>"))}");
    }

    // Only the admin who ran the command sees the answer.
    private static InteractionMessageProperties Reply(string content) =>
        new() { Content = content, Flags = MessageFlags.Ephemeral };
}
