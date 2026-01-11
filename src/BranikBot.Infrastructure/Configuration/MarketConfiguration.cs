using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace BranikBot.Infrastructure.Configuration;

public class MarketConfiguration
{
    public const string SectionName = "Market";

    [Required]
    public string Url { get; [UsedImplicitly] set; } = null!;

    [Required]
    public TimeSpan CacheDuration { get; [UsedImplicitly] set; }
}
