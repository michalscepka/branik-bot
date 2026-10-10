using System.Text;
using BranikBot.Parsing;
using BranikBot.Pricing;

namespace BranikBot.Replies;

public static class MessageFormatter
{
    public static string FormatMessage(IEnumerable<UserInput> userInputs, decimal marketPrice,
        IReadOnlyDictionary<Currency, decimal> exchangeRates, int maxAmounts, Random random)
    {
        var largest = userInputs
            .Select(input => (Input: input, ExchangeRate: TryGetExchangeRate(input.Currency, exchangeRates)))
            .Where(x => x.ExchangeRate is not null)
            .Select(x => (x.Input, ExchangeRate: x.ExchangeRate.GetValueOrDefault()))
            .OrderByDescending(x => x.Input.Amount * x.ExchangeRate)
            .Take(maxAmounts);

        var result = new StringBuilder();

        foreach (var (input, exchangeRate) in largest)
        {
            result.AppendLine($"> {input.OriginalText}");
            result.AppendLine(CreateMessageLine(input, input.Amount * exchangeRate, marketPrice, exchangeRate, random));
            result.AppendLine();
        }

        return result.ToString();
    }

    private static decimal? TryGetExchangeRate(Currency currency, IReadOnlyDictionary<Currency, decimal> exchangeRates)
    {
        if (currency is Currency.Czk)
            return 1m;

        return exchangeRates.TryGetValue(currency, out var rate) ? rate : null;
    }

    private static string CreateMessageLine(UserInput userInput, decimal valueInCzk, decimal marketPrice,
        decimal exchangeRate, Random random)
    {
        var (branikCount, parcelCount, palletsCount) = BranikCalculator.CalculateAmounts(valueInCzk, marketPrice);

        if (branikCount <= 0)
            return string.Format(Pick(Messages.InsufficientFunds, random), userInput.OriginalText);

        if (branikCount >= 100_000)
            return Pick(Messages.TooRich, random);

        var quantity = branikCount switch
        {
            < 100 =>
                $"{branikCount} {branikCount.GetBottleWord()}",
            < 1000 =>
                $"{parcelCount} {parcelCount.GetParcelWord()} ({branikCount} {branikCount.GetBottleWord()})",
            _ =>
                $"{palletsCount} {palletsCount.GetPalletWord()} ({parcelCount} {parcelCount.GetParcelWord()}) {branikCount.GetBottleWord()}"
        };

        var line = string.Format(Pick(Messages.Conversion, random), quantity);

        if (userInput.Currency is not Currency.Czk)
            line += string.Format(Messages.ExchangeRateNote, exchangeRate, Messages.GetOneUnit(userInput.Currency));

        return line;
    }

    private static string Pick(IReadOnlyList<string> phrasings, Random random)
    {
        return phrasings[random.Next(phrasings.Count)];
    }
}
