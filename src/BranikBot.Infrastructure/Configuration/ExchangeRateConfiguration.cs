using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace BranikBot.Infrastructure.Configuration;

public class ExchangeRateConfiguration
{
    public const string SectionName = "ExchangeRate";

    [Required]
    public string Url { get; [UsedImplicitly] set; } = null!;

    [Required]
    public TimeSpan CacheDuration { get; [UsedImplicitly] set; }
}
