using BranikBot.Infrastructure.Enums;

namespace BranikBot.Infrastructure.Models;

public record ParsedPrice(decimal Value, Currency Currency, string OriginalText);
