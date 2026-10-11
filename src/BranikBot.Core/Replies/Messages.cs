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

    // One extra fact follows every conversion, picked at random from those that apply. See ExtraFacts.
    // A fact says something the main sentence does not, so it comes from the money or the price,
    // never from the bottle count alone.

    // {0} is an amount of money. Like the exchange rate note, it is formatted by the app's culture (cs-CZ).
    public const string ChangeFact = "A ještě ti zbyde {0:C2}.";
    public const string NextBottleFact = "Do další ti chybí {0:C2}.";
    public const string HalfLitreFact = "Půllitr tě tak vyjde na {0:C2}.";
    public const string MissingFact = "Chybí ti ještě {0:C2}.";

    // {0} is a count and {1} the matching form of the word after it, in the accusative.
    public const string NextParcelFact = "Na další balík potřebuješ ještě {0} {1}.";
    public const string NextPalletFact = "Na další europaletu potřebuješ ještě {0} {1}.";

    public const string ExchangeRateNote = " Při aktuálním směnném kurzu ve výši {0:C2} za {1}.";

    // The bot's status line in Discord. {0} is the current price of a bottle.
    public const string Status = "Braník za {0:C2}";

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
