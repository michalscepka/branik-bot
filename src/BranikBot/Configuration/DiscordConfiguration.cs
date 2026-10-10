using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace BranikBot.Configuration;

public class DiscordConfiguration
{
    public const string SectionName = "Discord";

    [Required]
    public string BotToken { get; [UsedImplicitly] set; } = null!;

    [Required]
    public TimeSpan ChannelCooldown { get; [UsedImplicitly] set; }

    [Range(1, 10)]
    public int MaxAmountsPerReply { get; [UsedImplicitly] set; }
}
