using System.Collections.Concurrent;
using System.Text;
using BranikBot.Infrastructure.Helpers;
using BranikBot.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public class BranikMessageHandler : IMessageHandler
{
    private readonly GatewayClient _gatewayClient;
    private readonly IWebScrapingService _branikPriceService;
    private readonly ILogger<BranikMessageHandler> _logger;
    private readonly ConcurrentDictionary<ulong, DateTime> _lastMessageTimestamps;
    private readonly TimeSpan _cooldown = TimeSpan.FromMinutes(5); // TODO vzit z configu

    private const string Prefix = "To by stačilo na";
    private const string Postfix = "Braníčka ve slevě!";

    public BranikMessageHandler(GatewayClient gatewayClient, IWebScrapingService branikPriceService, ILogger<BranikMessageHandler> logger)
    {
        _gatewayClient = gatewayClient;
        _branikPriceService = branikPriceService;
        _logger = logger;
        _lastMessageTimestamps = new ConcurrentDictionary<ulong, DateTime>();
    }

    public async ValueTask ProcessIncomingMessage(Message message)
    {
        if (message.Author.IsBot)
            return;
        
        _logger.LogInformation("[{ChannelId}] {Username}: {Content}", message.ChannelId, message.Author.Username, message.Content);
        
        var now = DateTime.UtcNow;
        if (IsChannelOnCooldown(message.ChannelId, now))
            return;

        var prices = message.Content.ExtractPrices();
        if (prices is null || prices.Count is 0)
            return;

        var branikMarketPrice = await _branikPriceService.GetMarketPriceAsync();
        var chatMessage = new StringBuilder();
        ComposeBranikMessage(prices, chatMessage, branikMarketPrice);

        await _gatewayClient.Rest.SendMessageAsync(message.ChannelId, chatMessage.ToString());
        _lastMessageTimestamps.AddOrUpdate(message.ChannelId, now, (_, _) => now);
    }

    private bool IsChannelOnCooldown(ulong channelId, DateTime now)
    {
        _lastMessageTimestamps.TryGetValue(channelId, out var lastSent);
        
        if (now - lastSent < _cooldown)
        {
            _logger.LogInformation("Cooldown active for channel {ChannelId}, skipping response.", channelId);
            return true;
        }
        
        return false;
    }
    
    private void ComposeBranikMessage(Dictionary<decimal, string> prices, StringBuilder chatMessage, decimal branikMarketPrice)
    {
        foreach (var price in prices)
        {
            chatMessage.AppendLine($"> {price.Value}");
            var (branikCount, parcelCount, palletsCount, trucksCount) = BranikCalculator.CalculateAmounts(price.Key, branikMarketPrice);
            chatMessage.AppendLine(CreateBranikMessageLine(branikCount, parcelCount, palletsCount, trucksCount, price.Value));
            chatMessage.AppendLine();
        }
    }

    private string CreateBranikMessageLine(int branikCount, int parcelCount, int palletsCount, int trucksCount, string inputValue)
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
