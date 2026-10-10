using BranikBot.Pricing;

namespace BranikBot.Replies;

public static class Messages
{
    // Each list holds several phrasings of the same answer. One is picked at random for every answer.

    // {0} is how much Braník the amount buys, in the accusative: "5 dvoulitrovek", "2 baliky (12 dvoulitrovek)".
    public static readonly IReadOnlyList<string> Conversion =
    [
        "To by stačilo na {0} Braníčka ve slevě!",
        "Za to máš {0} Braníčka ve slevě!",
        "Za tohle koupíš {0} Braníčka ve slevě!",
        "Přepočteno na jedinou tvrdou měnu to dělá {0} Braníčka ve slevě!"
    ];

    // {0} is the amount as it was written in the message.
    public static readonly IReadOnlyList<string> InsufficientFunds =
    [
        "Lítost má v srdci, ale {0} nepostačí ani na jednu číši Braníčka plastového ve slevě.",
        "{0}? To nestačí ani na jednu dvoulitrovku Braníčka ve slevě.",
        "Za {0} ti nenalejou ani Braníčka ve slevě."
    ];

    public static readonly IReadOnlyList<string> TooRich =
    [
        "Za tolik by sis mohl koupit rovnou celý Plzeňský Prazdroj!",
        "S takovým balíkem se už nekupuje Braník, ale celý pivovar.",
        "Tolik Braníčka nemají ani ve skladu. Kup si rovnou pivovar!"
    ];

    public const string ExchangeRateNote = " Při aktuálním směnném kurzu ve výši {0:C2} za {1}.";

    // One unit of a foreign currency, as it reads after "za" in the exchange rate note.
    public static string GetOneUnit(Currency currency)
    {
        return currency switch
        {
            Currency.Eur => "jedno euro",
            Currency.Usd => "jeden dolar",
            Currency.Gbp => "jednu libru",
            Currency.Pln => "jeden zlotý",
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Not a foreign currency.")
        };
    }
}
