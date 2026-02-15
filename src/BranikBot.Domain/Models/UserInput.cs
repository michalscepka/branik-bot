using BranikBot.Domain.Enums;

namespace BranikBot.Domain.Models;

public record UserInput(decimal Amount, Currency Currency, string OriginalText);
