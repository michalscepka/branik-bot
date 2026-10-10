using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace BranikBot.Configuration;

public class DatabaseConfiguration
{
    public const string SectionName = "Database";

    [Required]
    public string Path { get; [UsedImplicitly] set; } = null!;
}
