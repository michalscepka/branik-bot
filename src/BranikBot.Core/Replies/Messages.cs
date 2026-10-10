using BranikBot.Pricing;

namespace BranikBot.Replies;

public static class Messages
{
    public const string InsufficientFunds = "Lítost má v srdci, ale {0} nepostačí ani na jednu číši Braníčka plastového ve slevě.";
    public const string Prefix = "To by stačilo na";
    public const string Postfix = "Braníčka ve slevě!";
    public const string ExchangeRateNote = " Při aktuálním směnném kurzu ve výši {0:C2} za {1}.";
    public const string TooRich = "Za tolik by sis mohl koupit rovnou celý Plzeňský Prazdroj!";

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
