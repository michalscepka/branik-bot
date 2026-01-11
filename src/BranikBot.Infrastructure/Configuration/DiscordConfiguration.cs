using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace BranikBot.Infrastructure.Configuration;

public class DiscordConfiguration
{
    public const string SectionName = "Discord";

    [Required]
    public string BotToken { get; [UsedImplicitly] set; } = null!;

    [Required]
    public TimeSpan ChannelCooldown { get; [UsedImplicitly] set; }
}
