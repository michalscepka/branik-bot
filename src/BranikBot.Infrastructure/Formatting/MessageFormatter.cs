using System.Text;
using BranikBot.Application.Providers;
using BranikBot.Application.Formatting;
using BranikBot.Application.Resources;
using BranikBot.Domain;
using BranikBot.Domain.Enums;
using BranikBot.Domain.Models;
using BranikBot.Infrastructure.Extensions;

namespace BranikBot.Infrastructure.Formatting;

public class MessageFormatter(IExchangeRateProvider exchangeRateClient) : IMessageFormatter
{
    public async ValueTask<string> FormatMessageAsync(IEnumerable<UserInput> userInputs, decimal marketPrice)
    {
        var result = new StringBuilder();
        var inputs = userInputs.ToList();
        var exchangeRate = 0m;

        if (inputs.Any(p => p.Currency == Currency.Eur))
            exchangeRate = await exchangeRateClient.GetExchangeRateAsync(Currency.Eur);

        foreach (var input in inputs)
        {
            result.AppendLine($"> {input.OriginalText}");

            var finalAmount = input.Amount;

            if (input.Currency is Currency.Eur)
                finalAmount *= exchangeRate;

            result.AppendLine(CreateMessageLine(input, finalAmount, marketPrice, exchangeRate));
            result.AppendLine();
        }

        return result.ToString();
    }

    private string CreateMessageLine(UserInput userInput, decimal valueInCzk, decimal marketPrice, decimal exchangeRate)
    {
        var (branikCount, parcelCount, palletsCount) = BranikCalculator.CalculateAmounts(valueInCzk, marketPrice);

        if (branikCount <= 0)
            return string.Format(Messages.InsufficientFunds, userInput.OriginalText);

        var postfix = Messages.Postfix;
        if (userInput.Currency is not Currency.Czk)
            postfix += string.Format(Messages.EuroExchangeRateNote, exchangeRate);

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
