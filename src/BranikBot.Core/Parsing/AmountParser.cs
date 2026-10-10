using System.Globalization;
using System.Text;
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

    // Every word list below is written without diacritics, because the message is searched without them:
    // "kc" also covers "kč", "tisic" covers "tisíc" and "milion" covers "milión".

    private static readonly Dictionary<Currency, string[]> CurrencyDefinitions = new()
    {
        { Currency.Eur, ["eur", "euro", "eura", "€"] },
        { Currency.Czk, ["kc", "czk", "korun", "koruny", "koruna", ",-"] }
    };

    // Need a number in front: "20 tisic", "2 tace", "3 melouny".
    // Words must match exactly. Abbreviations may also be followed by a dot ("tis.", "mil.").
    private static readonly MultiplierDefinition[] MultiplierDefinitions =
    [
        new(1_000,
            Words: ["k", "tisic", "tisice", "tisicu", "tac", "tace", "tacu"],
            Abbreviations: ["tis"]),
        new(1_000_000,
            Words: ["mega", "milion", "miliony", "milionu", "meloun", "melouny", "melounu"],
            Abbreviations: ["mil", "mio"]),
        new(1_000_000_000,
            Words: ["miliarda", "miliardy", "miliard"],
            Abbreviations: ["mld"])
    ];

    // Slang for a coin or a banknote. The singular is an amount on its own ("dej mi stovku"),
    // the plural only counts with a number ("5 stovek"), because "stovky lidi" is not money.
    private static readonly DenominationDefinition[] DenominationDefinitions =
    [
        new(20, Singular: ["dvacka", "dvacku"], Plural: ["dvacky", "dvacek"]),
        new(100, Singular: ["stovka", "stovku"], Plural: ["stovky", "stovek"]),
        new(200, Singular: ["dvoustovka", "dvoustovku"], Plural: ["dvoustovky", "dvoustovek"]),
        new(500, Singular: ["petistovka", "petistovku"], Plural: ["petistovky", "petistovek"]),
        new(1_000, Singular: ["tisicovka", "tisicovku"], Plural: ["tisicovky", "tisicovek"]),
        new(2_000, Singular: ["dvoutisicovka", "dvoutisicovku"], Plural: ["dvoutisicovky", "dvoutisicovek"]),
        new(5_000, Singular: ["petitisicovka", "petitisicovku"], Plural: ["petitisicovky", "petitisicovek"])
    ];

    private static readonly Dictionary<string, decimal> Factors = CreateFactors();

    private static readonly string CurrencySuffixPattern =
        JoinAlternatives(CurrencyDefinitions.Values.SelectMany(suffixes => suffixes));

    private static readonly string CountedWordPattern = JoinAlternatives(
        MultiplierDefinitions.SelectMany(definition => definition.Words)
            .Concat(DenominationDefinitions.SelectMany(definition => definition.Singular))
            .Concat(DenominationDefinitions.SelectMany(definition => definition.Plural)));

    private static readonly string AbbreviationPattern =
        JoinAlternatives(MultiplierDefinitions.SelectMany(definition => definition.Abbreviations));

    private static readonly string StandaloneWordPattern =
        JoinAlternatives(DenominationDefinitions.SelectMany(definition => definition.Singular));

    // Two shapes of an amount:
    //   1. A number followed by a word, a currency or both: "20 tisic", "5 stovek", "100 kc", "1,5 mil. Kc".
    //   2. A denomination on its own, with an optional currency: "stovka", "petistovka euro".
    // The \b keeps a word from matching a part of a longer one ("k" in "kc", "mil" in "mily").
    private static readonly string Pattern =
        $"""
         (?<{Number}>{NumberPattern})\s*
         (?:
             (?<{Multiplier}>(?:{CountedWordPattern})\b|(?:{AbbreviationPattern})\b\.?)
             (?:\s*(?<{CurrencySuffix}>{CurrencySuffixPattern}))?
             |
             (?<{CurrencySuffix}>{CurrencySuffixPattern})
         )
         |
         \b(?<{Multiplier}>{StandaloneWordPattern})\b
         (?:\s*(?<{CurrencySuffix}>{CurrencySuffixPattern}))?
         """;

    private static readonly Regex BranikRegex = new(
        Pattern,
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace,
        TimeSpan.FromSeconds(1));

    public static IEnumerable<UserInput> ExtractAmounts(this string message)
    {
        var result = new HashSet<UserInput>();

        // Same length as the message, so a match position is also a position in the message.
        var searchableText = RemoveDiacritics(message);

        foreach (Match match in BranikRegex.Matches(searchableText))
        {
            var amount = TryGetAmount(match);
            if (amount is null)
                continue;

            var currency = GetCurrencyFromSuffix(match.Groups[CurrencySuffix].Value);
            var originalText = message.Substring(match.Index, match.Length);
            result.Add(new UserInput(amount.Value, currency, originalText));
        }

        return result;
    }

    private static decimal? TryGetAmount(Match match)
    {
        var number = TryGetNumber(match.Groups[Number]);
        var factor = TryGetFactor(match.Groups[Multiplier]);

        if (number is null || factor is null)
            return null;

        return number > MaxAmount / factor ? MaxAmount : number * factor;
    }

    private static decimal? TryGetNumber(Group number)
    {
        if (!number.Success)
            return 1;

        return TryParseDecimal(number.Value, out var value) ? value : null;
    }

    private static decimal? TryGetFactor(Group multiplier)
    {
        if (!multiplier.Success)
            return 1;

        var text = multiplier.Value.TrimEnd('.');
        return Factors.TryGetValue(text, out var factor) ? factor : null;
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

    private static string RemoveDiacritics(string text)
    {
        var result = new StringBuilder(text.Length);

        foreach (var character in text)
            result.Append(RemoveDiacritics(character));

        return result.ToString();
    }

    private static char RemoveDiacritics(char character)
    {
        // Half of an emoji cannot be decomposed on its own.
        if (char.IsAscii(character) || char.IsSurrogate(character))
            return character;

        // Decomposing splits "č" into "c" and the caron. The first character is the plain letter.
        return character.ToString().Normalize(NormalizationForm.FormD)[0];
    }

    private static Dictionary<string, decimal> CreateFactors()
    {
        var factors = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in MultiplierDefinitions)
        {
            foreach (var text in definition.Words.Concat(definition.Abbreviations))
                factors.Add(text, definition.Factor);
        }

        foreach (var definition in DenominationDefinitions)
        {
            foreach (var text in definition.Singular.Concat(definition.Plural))
                factors.Add(text, definition.Value);
        }

        return factors;
    }

    private static string JoinAlternatives(IEnumerable<string> texts)
    {
        return string.Join("|", texts.OrderByDescending(text => text.Length).Select(Regex.Escape));
    }

    private sealed record MultiplierDefinition(decimal Factor, string[] Words, string[] Abbreviations);

    private sealed record DenominationDefinition(decimal Value, string[] Singular, string[] Plural);
}
