using System.Globalization;
using System.Text.RegularExpressions;
using BranikBot.Infrastructure.Enums;
using BranikBot.Infrastructure.Models;

namespace BranikBot.Infrastructure.Helpers;

public static class PriceParser
{
    private const string Pattern =
        @"(?<Ones>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*(?<CurrencySuffix>((kc|kč|czk|korun|koruny|koruna|eur|euro|eura)\b)|€|,-)|(?<Thousands>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*k\b|(?<Millions>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*mega\b";

    private static readonly Regex BranikRegex = new (Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public static IEnumerable<ParsedPrice> ExtractPrices(this string message)
    {
        var matches = BranikRegex.Matches(message);

        var result = new List<ParsedPrice>();

        if (matches.Count is 0)
            return result;

        foreach (Match match in matches)
        {
            var originalValue = match.Groups[0].Value;
            decimal? value = null;
            var currency = Currency.Czk;

            if (match.Groups[nameof(PriceGroup.Ones)].Success)
            {
                value = ParseDecimal(match.Groups[nameof(PriceGroup.Ones)].Value);
                var suffix = match.Groups["CurrencySuffix"].Value;
                if (suffix.Contains("eur", StringComparison.OrdinalIgnoreCase) || suffix.Contains("€"))
                {
                    currency = Currency.Eur;
                }
            }

            if (match.Groups[nameof(PriceGroup.Thousands)].Success)
                value = ParseDecimal(match.Groups[nameof(PriceGroup.Thousands)].Value) * 1_000;

            if (match.Groups[nameof(PriceGroup.Millions)].Success)
                value = ParseDecimal(match.Groups[nameof(PriceGroup.Millions)].Value) * 1_000_000;

            if (value.HasValue)
            {
                // Deduplicate based on Value and Currency
                if (!result.Any(p => p.Value == value.Value && p.Currency == currency))
                {
                    result.Add(new ParsedPrice(value.Value, currency, originalValue));
                }
            }
        }

        return result;
    }

    private static decimal ParseDecimal(string value)
    {
        var normalized = value.Replace(" ", "").Replace(",", ".");
        return decimal.Parse(normalized, CultureInfo.InvariantCulture);
    }
}
