using BranikBot.Domain.Enums;

namespace BranikBot.Domain.Models;

public record ParsedPrice(decimal Amount, Currency Currency, string OriginalText);
