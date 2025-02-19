using System.Text;
using BranikBot.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public class BranikMessageEventHandler : IMessageEventHandler
{
    private readonly GatewayClient _gatewayClient;
    private readonly IWebScrapingService _branikPriceService;
    private readonly ILogger<BranikMessageEventHandler> _logger;

    private const string Prefix = "To by stačilo na";
    private const string Postfix = "Braníčka ve slevě!";

    public BranikMessageEventHandler(GatewayClient gatewayClient, IWebScrapingService branikPriceService, ILogger<BranikMessageEventHandler> logger)
    {
        _gatewayClient = gatewayClient;
        _branikPriceService = branikPriceService;
        _logger = logger;
    }

    public async ValueTask HandleMessage(Message message)
    {
        if (message.Author.IsBot)
            return;

        _logger.LogInformation("[{ChannelId}] {Username}: {Content}", message.ChannelId, message.Author.Username, message.Content);

        var prices = PriceParser.ExtractPrices(message.Content);
        
        if (prices is null || prices.Count is 0)
            return;

        var branikMarketPrice = await _branikPriceService.GetMarketPriceAsync();

        var chatMessage = new StringBuilder();
        
        foreach (var price in prices)
        {
            chatMessage.AppendLine($"> {price.Value}");
            var (branikCount, parcelCount, palletsCount, trucksCount) = BranikCalculator.CalculateAmounts(price.Key, branikMarketPrice);
            chatMessage.AppendLine(GenerateBranikMessage(branikCount, parcelCount, palletsCount, trucksCount, price.Value));
            chatMessage.AppendLine();
        }

        await _gatewayClient.Rest.SendMessageAsync(message.ChannelId, chatMessage.ToString());
    }

    private string GenerateBranikMessage(int branikCount, int parcelCount, int palletsCount, int trucksCount, string inputValue)
    {
        if (branikCount <= 0)
            return $"Lítost má v srdci, ale {inputValue} nepostačí ani na jednu číši Braníčka plastového ve slevě.";

        return branikCount switch
        {
            < 100 => 
                $"{Prefix} {branikCount} {branikCount.GetBottleWord()} {Postfix}",
            < 1000 =>
                $"{Prefix} {parcelCount} {parcelCount.GetParcelWord()} ({branikCount} {branikCount.GetBottleWord()}) {Postfix}",
            < 100_000 =>
                $"{Prefix} {palletsCount} {palletsCount.GetPalletWord()} ({parcelCount} {parcelCount.GetParcelWord()}) {branikCount.GetBottleWord()} {Postfix}",
            _ =>
                $"{Prefix} {trucksCount} {trucksCount.GetTruckWord()} ({palletsCount} {palletsCount.GetPalletWord()}) {branikCount.GetBottleWord()} {Postfix}"
        };
    }
}
