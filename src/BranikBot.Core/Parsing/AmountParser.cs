using System.Globalization;
using System.Text.RegularExpressions;
using BranikBot.Pricing;

namespace BranikBot.Parsing;

public static class AmountParser
{
    private const string NumberPattern = @"\d{1,3}(\s?\d{3})*([.,]\d+)?";
    private const string Number = "Number";
    private const string Multiplier = "Multiplier";
    private const string CurrencySuffix = "CurrencySuffix";

    // Anything above this is answered the same way, and capping it keeps later calculations from overflowing.
    private const decimal MaxAmount = 1_000_000_000_000_000_000m;

    private static readonly Dictionary<Currency, string[]> CurrencyDefinitions = new()
    {
        { Currency.Eur, ["eur", "euro", "eura", "€"] },
        { Currency.Czk, ["kc", "kč", "czk", "korun", "koruny", "koruna", ",-"] }
    };

    // Words must match exactly. Abbreviations may also be followed by a dot ("tis.", "mil.").
    private static readonly MultiplierDefinition[] MultiplierDefinitions =
    [
        new(1_000,
            Words: ["k", "tisíc", "tisíce", "tisíců", "tisic", "tisice", "tisicu"],
            Abbreviations: ["tis"]),
        new(1_000_000,
            Words: ["mega", "milion", "miliony", "milionů", "milionu", "milión", "milióny", "miliónů"],
            Abbreviations: ["mil", "mio"]),
        new(1_000_000_000,
            Words: ["miliarda", "miliardy", "miliard"],
            Abbreviations: ["mld"])
    ];

    private static readonly Dictionary<string, decimal> MultiplierFactors = MultiplierDefinitions
        .SelectMany(definition => definition.Words
            .Concat(definition.Abbreviations)
            .Select(text => (Text: text, definition.Factor)))
        .ToDictionary(x => x.Text, x => x.Factor, StringComparer.OrdinalIgnoreCase);

    private static readonly string CurrencySuffixPattern =
        JoinAlternatives(CurrencyDefinitions.Values.SelectMany(suffixes => suffixes));

    private static readonly string MultiplierWordPattern =
        JoinAlternatives(MultiplierDefinitions.SelectMany(definition => definition.Words));

    private static readonly string MultiplierAbbreviationPattern =
        JoinAlternatives(MultiplierDefinitions.SelectMany(definition => definition.Abbreviations));

    // A number followed by a multiplier, a currency or both: "20 tisíc", "100 kč", "1,5 mil. Kč".
    // The \b keeps a multiplier from matching the start of a longer word ("k" in "kč", "mil" in "milý").
    private static readonly string Pattern =
        $"""
         (?<{Number}>{NumberPattern})\s*
         (?:
             (?<{Multiplier}>(?:{MultiplierWordPattern})\b|(?:{MultiplierAbbreviationPattern})\b\.?)
             (?:\s*(?<{CurrencySuffix}>{CurrencySuffixPattern}))?
             |
             (?<{CurrencySuffix}>{CurrencySuffixPattern})
         )
         """;

    private static readonly Regex BranikRegex = new(
        Pattern,
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace,
        TimeSpan.FromSeconds(1));

    public static IEnumerable<UserInput> ExtractAmounts(this string message)
    {
        var result = new HashSet<UserInput>();

        foreach (Match match in BranikRegex.Matches(message))
        {
            var amount = TryGetAmount(match);
            if (amount is null)
                continue;

            var currency = GetCurrencyFromSuffix(match.Groups[CurrencySuffix].Value);
            result.Add(new UserInput(amount.Value, currency, match.Value));
        }

        return result;
    }

    private static decimal? TryGetAmount(Match match)
    {
        if (!TryParseDecimal(match.Groups[Number].Value, out var number))
            return null;

        var factor = TryGetFactor(match.Groups[Multiplier]);
        if (factor is null)
            return null;

        return number > MaxAmount / factor.Value ? MaxAmount : number * factor.Value;
    }

    private static decimal? TryGetFactor(Group multiplier)
    {
        if (!multiplier.Success)
            return 1;

        var text = multiplier.Value.TrimEnd('.');
        return MultiplierFactors.TryGetValue(text, out var factor) ? factor : null;
    }

    private static string JoinAlternatives(IEnumerable<string> texts)
    {
        return string.Join("|", texts.OrderByDescending(text => text.Length).Select(Regex.Escape));
    }

    private static Currency GetCurrencyFromSuffix(string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
            return Currency.Czk;

        var normalizedSuffix = suffix.Trim();

        foreach (var (currency, patterns) in CurrencyDefinitions)
        {
            if (patterns.Any(p => string.Equals(p, normalizedSuffix, StringComparison.OrdinalIgnoreCase)))
                return currency;
        }

        return Currency.Czk;
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        var normalized = value.Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private sealed record MultiplierDefinition(decimal Factor, string[] Words, string[] Abbreviations);
}
