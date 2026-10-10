using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace BranikBot.Configuration;

public class ExchangeRateConfiguration
{
    public const string SectionName = "ExchangeRate";

    [Required]
    public string Url { get; [UsedImplicitly] set; } = null!;

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00", ParseLimitsInInvariantCulture = true)]
    public TimeSpan SampleInterval { get; [UsedImplicitly] set; }
}
