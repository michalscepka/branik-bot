using BranikBot.Pricing;

namespace BranikBot.Parsing;

public record UserInput(decimal Amount, Currency Currency, string OriginalText);
