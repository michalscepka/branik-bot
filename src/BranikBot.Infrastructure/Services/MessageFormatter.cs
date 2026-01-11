using System.Text;
using BranikBot.Infrastructure.Enums;
using BranikBot.Infrastructure.Helpers;
using BranikBot.Infrastructure.Models;
using BranikBot.Infrastructure.Resources;
using BranikBot.Infrastructure.Services.Abstractions;

namespace BranikBot.Infrastructure.Services;

public class MessageFormatter : IMessageFormatter
{
    private readonly IExchangeRateService _exchangeRateService;

    public MessageFormatter(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    public async Task<string> FormatMessageAsync(IEnumerable<ParsedPrice> prices, decimal marketPrice)
    {
        var result = new StringBuilder();
        var pricesList = prices.ToList();

        var exchangeRateTasks = pricesList
            .Select(p => p.Currency)
            .Distinct()
            .ToDictionary(c => c, c => _exchangeRateService.GetExchangeRateAsync(c));

        await Task.WhenAll(exchangeRateTasks.Values);

        foreach (var parsedPrice in pricesList)
        {
            result.AppendLine($"> {parsedPrice.OriginalText}");

            var exchangeRate = exchangeRateTasks[parsedPrice.Currency].Result;

            var valueInCzk = parsedPrice.Amount * exchangeRate;
            result.AppendLine(CreateMessageLine(parsedPrice, valueInCzk, marketPrice, exchangeRate));
            result.AppendLine();
        }

        return result.ToString();
    }

    private string CreateMessageLine(ParsedPrice parsedPrice, decimal valueInCzk, decimal marketPrice, decimal exchangeRate)
    {
        var (branikCount, parcelCount, palletsCount) = BranikCalculator.CalculateAmounts(valueInCzk, marketPrice);

        if (branikCount <= 0)
            return string.Format(Messages.InsufficientFunds, parsedPrice.OriginalText);

        var prefix = Messages.Prefix;

        var postfix = Messages.Postfix;
        if (parsedPrice.Currency is not Currency.Czk)
            postfix += string.Format(Messages.EuroExchangeRateNote, exchangeRate);

        return branikCount switch
        {
            < 100 =>
                $"{prefix} {branikCount} {branikCount.GetBottleWord()} {postfix}",
            < 1000 =>
                $"{prefix} {parcelCount} {parcelCount.GetParcelWord()} ({branikCount} {branikCount.GetBottleWord()}) {postfix}",
            < 100_000 =>
                $"{prefix} {palletsCount} {palletsCount.GetPalletWord()} ({parcelCount} {parcelCount.GetParcelWord()}) {branikCount.GetBottleWord()} {postfix}",
            _ =>
                Messages.TooRich
        };
    }
}
