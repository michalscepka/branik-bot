using System.Globalization;
using System.Text.RegularExpressions;
using BranikBot.Infrastructure.Enums;

namespace BranikBot.Infrastructure.Helpers;

public static class PriceParser
{
    private const string Pattern =
        @"(?<Ones>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*((kc|kč|czk|korun|koruny|koruna)\b|,-)|(?<Thousands>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*k\b|(?<Millions>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*mega\b";

    private static readonly Regex BranikRegex = new (Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public static Dictionary<decimal, string> ExtractPrices(this string message)
    {
        var matches = BranikRegex.Matches(message);

        var result = new Dictionary<decimal, string>();

        if (matches.Count is 0)
            return result;

        foreach (Match match in matches)
        {
            var originalValue = match.Groups[0].Value;

            if (match.Groups[nameof(PriceGroup.Ones)].Success)
                result.TryAdd(ParseDecimal(match.Groups[nameof(PriceGroup.Ones)].Value), originalValue);

            if (match.Groups[nameof(PriceGroup.Thousands)].Success)
                result.TryAdd(ParseDecimal(match.Groups[nameof(PriceGroup.Thousands)].Value) * 1_000, originalValue);

            if (match.Groups[nameof(PriceGroup.Millions)].Success)
                result.TryAdd(ParseDecimal(match.Groups[nameof(PriceGroup.Millions)].Value) * 1_000_000, originalValue);
        }

        return result;
    }

    private static decimal ParseDecimal(string value)
    {
        var normalized = value.Replace(" ", "").Replace(",", ".");
        return decimal.Parse(normalized, CultureInfo.InvariantCulture);
    }
}
