namespace BranikBot.Replies;

// Short facts that follow the main sentence of an answer.
public static class ExtraFacts
{
    private const int HalfLitresPerBottle = 4;

    // Bottles are counted in parcels only from this many up, the same limit MessageFormatter uses.
    private const int FirstBottleCountInParcels = 100;

    // Every fact that can be said about buying this many bottles. Never empty.
    public static IReadOnlyList<string> ForConversion(int bottleCount, decimal valueInCzk, decimal bottlePrice)
    {
        var change = Round(valueInCzk - bottleCount * bottlePrice);

        var facts = new List<string>
        {
            string.Format(Messages.NextBottleFact, bottlePrice - change),
            string.Format(Messages.HalfLitreFact, bottlePrice / HalfLitresPerBottle),
            CreateNextUnitFact(bottleCount)
        };

        if (change > 0)
            facts.Add(string.Format(Messages.ChangeFact, change));

        return facts;
    }

    // How much is missing for one bottle, or null when the difference is less than one haléř.
    public static string? ForInsufficientFunds(decimal valueInCzk, decimal bottlePrice)
    {
        var missing = Round(bottlePrice - valueInCzk);
        return missing > 0 ? string.Format(Messages.MissingFact, missing) : null;
    }

    // What is needed to fill the next bigger unit: bottles for a parcel, or parcels for a pallet.
    private static string CreateNextUnitFact(int bottleCount)
    {
        if (bottleCount < FirstBottleCountInParcels)
        {
            var bottles = MissingToNextFull(bottleCount, BranikCalculator.ParcelSize);
            return string.Format(Messages.NextParcelFact, bottles, bottles.GetBottleWord());
        }

        var parcels = MissingToNextFull(bottleCount / BranikCalculator.ParcelSize, BranikCalculator.PalletSize);
        return string.Format(Messages.NextPalletFact, parcels, parcels.GetParcelWord());
    }

    // A count that already fills a unit exactly still needs a whole new one.
    private static int MissingToNextFull(int count, int unitSize) => unitSize - count % unitSize;

    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
