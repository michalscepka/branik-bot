using System.Globalization;
using System.Text.RegularExpressions;
using BranikBot.Infrastructure.Enums;
using BranikBot.Infrastructure.Models;

namespace BranikBot.Infrastructure.Helpers;

public static class PriceParser
{
    private const string NumberPattern = @"\d{1,3}(\s?\d{3})*([.,]\d+)?";
    private const string CurrencySuffix = "CurrencySuffix";
    private const string Base = "Base";
    private const string Thousands = "Thousands";
    private const string Millions = "Millions";

    private static readonly Dictionary<Currency, string[]> CurrencyDefinitions = new()
    {
        { Currency.Eur, ["eur", "euro", "eura", "€"] },
        { Currency.Czk, ["kc", "kč", "czk", "korun", "koruny", "koruna", ",-"] }
    };

    private static readonly string CurrencySuffixPattern =
        string.Join("|", CurrencyDefinitions.Values
            .SelectMany(v => v)
            .OrderByDescending(v => v.Length)
            .Select(Regex.Escape));

    private static readonly string Pattern =
        $"""
         (?<{Base}>{NumberPattern})\s*(?<{CurrencySuffix}>{CurrencySuffixPattern})|
         (?<{Thousands}>{NumberPattern})\s*k\b(\s*(?<{CurrencySuffix}>{CurrencySuffixPattern}))?|
         (?<{Millions}>{NumberPattern})\s*mega\b(\s*(?<{CurrencySuffix}>{CurrencySuffixPattern}))?
         """;

    private static readonly Regex BranikRegex = new(
        Pattern,
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace,
        TimeSpan.FromSeconds(1));

    public static IEnumerable<ParsedPrice> ExtractPrices(this string message)
    {
        var matches = BranikRegex.Matches(message);
        var result = new HashSet<ParsedPrice>();

        if (matches.Count is 0)
            return result;

        foreach (Match match in matches)
        {
            var originalValue = match.Groups[0].Value;
            var currency = GetCurrencyFromSuffix(match.Groups[CurrencySuffix].Value);

            var amount = TryGetAmount(Base, 1, match) ??
                        TryGetAmount(Thousands, 1_000, match) ??
                        TryGetAmount(Millions, 1_000_000, match);

            if (amount.HasValue)
                result.Add(new ParsedPrice(amount.Value, currency, originalValue));
        }

        return result;
    }

    private static decimal? TryGetAmount(string groupName, decimal multiplier, Match match)
    {
        return match.Groups[groupName].Success &&
               TryParseDecimal(match.Groups[groupName].Value, out var val)
            ? val * multiplier
            : null;
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
}
