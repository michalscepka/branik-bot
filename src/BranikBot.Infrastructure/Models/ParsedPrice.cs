using BranikBot.Infrastructure.Enums;

namespace BranikBot.Infrastructure.Models;

public record ParsedPrice(decimal Amount, Currency Currency, string OriginalText);
