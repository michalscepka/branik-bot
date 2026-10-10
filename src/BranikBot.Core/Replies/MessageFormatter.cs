using System.Text;
using BranikBot.Parsing;
using BranikBot.Pricing;

namespace BranikBot.Replies;

public static class MessageFormatter
{
    public static string FormatMessage(IEnumerable<UserInput> userInputs, decimal marketPrice,
        IReadOnlyDictionary<Currency, decimal> exchangeRates, int maxAmounts)
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
            result.AppendLine(CreateMessageLine(input, input.Amount * exchangeRate, marketPrice, exchangeRate));
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

    private static string CreateMessageLine(UserInput userInput, decimal valueInCzk, decimal marketPrice, decimal exchangeRate)
    {
        var (branikCount, parcelCount, palletsCount) = BranikCalculator.CalculateAmounts(valueInCzk, marketPrice);

        if (branikCount <= 0)
            return string.Format(Messages.InsufficientFunds, userInput.OriginalText);

        var postfix = Messages.Postfix;
        if (userInput.Currency is not Currency.Czk)
            postfix += string.Format(Messages.ExchangeRateNote, exchangeRate, Messages.GetOneUnit(userInput.Currency));

        return branikCount switch
        {
            < 100 =>
                $"{Messages.Prefix} {branikCount} {branikCount.GetBottleWord()} {postfix}",
            < 1000 =>
                $"{Messages.Prefix} {parcelCount} {parcelCount.GetParcelWord()} ({branikCount} {branikCount.GetBottleWord()}) {postfix}",
            < 100_000 =>
                $"{Messages.Prefix} {palletsCount} {palletsCount.GetPalletWord()} ({parcelCount} {parcelCount.GetParcelWord()}) {branikCount.GetBottleWord()} {postfix}",
            _ =>
                Messages.TooRich
        };
    }
}
