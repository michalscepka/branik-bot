using System.Text.RegularExpressions;
using BranikBot.Infrastructure.Enums;

namespace BranikBot.Infrastructure.Helpers;

public static class PriceParser
{
    private const string Pattern =
        @"(?<Ones>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*(kc|kč|,-|czk|korun|koruny|koruna)\b|(?<Thousands>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*k\b|(?<Millions>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*mega\b";

    private static readonly Regex BranikRegex = new (Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public static Dictionary<decimal, string>? ExtractPrices(this string message)
    {
        var matches = BranikRegex.Matches(message);
        
        if (matches.Count is 0)
            return null;

        var result = new Dictionary<decimal, string>();
        
        foreach (Match match in matches)
        {
            var quoteValue = match.Groups[0].Value;
            
            if (match.Groups[nameof(PriceGroup.Ones)].Success)
                result.TryAdd(decimal.Parse(match.Groups[nameof(PriceGroup.Ones)].Value), quoteValue);
            
            if (match.Groups[nameof(PriceGroup.Thousands)].Success)
                result.TryAdd(decimal.Parse(match.Groups[nameof(PriceGroup.Thousands)].Value) * 1_000, quoteValue);
            
            if (match.Groups[nameof(PriceGroup.Millions)].Success)
                result.TryAdd(decimal.Parse(match.Groups[nameof(PriceGroup.Millions)].Value) * 1_000_000, quoteValue);
        }

        return result;
    }
}
