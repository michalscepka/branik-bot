using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace BranikBot.Infrastructure.Configuration;

public class CashingConfiguration
{
    public const string SectionName = "Caching";

    [Required]
    public string MarketPriceUrl { get; [UsedImplicitly] set; } = null!;
    
    [Required]
    public TimeSpan DurationMinutes { get; [UsedImplicitly] set; }
}
