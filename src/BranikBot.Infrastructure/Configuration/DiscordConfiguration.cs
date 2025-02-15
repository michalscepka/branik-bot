using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace BranikBot.Infrastructure.Configuration;

public class DiscordConfiguration
{
    public const string SectionName = "Discord";

    [Required]
    public string Token { get; [UsedImplicitly] set; } = null!;
}
