using System.Collections.Concurrent;
using System.Text;
using BranikBot.Infrastructure.Helpers;
using BranikBot.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public class MessageHandler : IMessageHandler
{
    private readonly GatewayClient _gatewayClient;
    private readonly IPriceService _branikPriceService;
    private readonly ILogger<MessageHandler> _logger;
    private readonly ConcurrentDictionary<ulong, DateTime> _lastMessageTimestamps;
    private readonly TimeSpan _cooldown = TimeSpan.FromMinutes(5); // TODO vzit z configu

    public MessageHandler(GatewayClient gatewayClient, IPriceService branikPriceService, ILogger<MessageHandler> logger)
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

        var utcNow = DateTime.UtcNow;
        if (IsChannelOnCooldown(message.ChannelId, utcNow))
            return;

        var prices = message.Content.ExtractPrices();
        if (prices.Count <= 0)
            return;

        var branikMarketPrice = await _branikPriceService.GetMarketPriceAsync();
        var chatMessage = CreateBranikMessage(prices, branikMarketPrice);

        await _gatewayClient.Rest.SendMessageAsync(message.ChannelId, chatMessage);
        _lastMessageTimestamps.AddOrUpdate(message.ChannelId, utcNow, (_, _) => utcNow);
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

    private string CreateBranikMessage(Dictionary<decimal, string> prices, decimal branikMarketPrice)
    {
        var result = new StringBuilder();

        foreach (var price in prices)
        {
            result.AppendLine($"> {price.Value}");
            result.AppendLine(CreateBranikMessageLine(price, branikMarketPrice));
            result.AppendLine();
        }

        return result.ToString();
    }

    private string CreateBranikMessageLine(KeyValuePair<decimal, string> priceKvp, decimal branikMarketPrice)
    {
        var (branikCount, parcelCount, palletsCount, trucksCount) = BranikCalculator.CalculateAmounts(priceKvp.Key, branikMarketPrice);

        if (branikCount <= 0)
            return $"Lítost má v srdci, ale {priceKvp.Value} nepostačí ani na jednu číši Braníčka plastového ve slevě.";

        const string prefix = "To by stačilo na";
        const string postfix = "Braníčka ve slevě!";

        return branikCount switch
        {
            < 100 =>
                $"{prefix} {branikCount} {branikCount.GetBottleWord()} {postfix}",
            < 1000 =>
                $"{prefix} {parcelCount} {parcelCount.GetParcelWord()} ({branikCount} {branikCount.GetBottleWord()}) {postfix}",
            < 100_000 =>
                $"{prefix} {palletsCount} {palletsCount.GetPalletWord()} ({parcelCount} {parcelCount.GetParcelWord()}) {branikCount.GetBottleWord()} {postfix}",
            _ =>
                $"{prefix} {trucksCount} {trucksCount.GetTruckWord()} ({palletsCount} {palletsCount.GetPalletWord()}) {branikCount.GetBottleWord()} {postfix}"
        };
    }
}
