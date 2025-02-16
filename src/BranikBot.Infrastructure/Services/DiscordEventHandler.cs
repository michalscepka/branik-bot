using System.Text;
using System.Text.RegularExpressions;
using BranikBot.Infrastructure.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public partial class DiscordEventHandler : BackgroundService
{
    private readonly GatewayClient _gatewayClient;
    private readonly ILogger<DiscordEventHandler> _logger;

    private const string Pattern =
        @"(?<Ones>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*(kc|kč|,-|czk|korun)|(?<Thousands>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*k\b|(?<Millions>\d{1,3}(\s?\d{3})*([.,]\d+)?)\s*mega";

    private const int BranikPrice = 44;

    public DiscordEventHandler(GatewayClient gatewayClient, ILogger<DiscordEventHandler> logger)
    {
        _gatewayClient = gatewayClient;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DiscordEventHandler is starting...");

        _gatewayClient.MessageCreate += OnMessageReceived;

        return Task.CompletedTask;
    }

    private async ValueTask OnMessageReceived(Message message)
    {
        if (message.Author.IsBot)
            return;

        _logger.LogInformation("[{message.ChannelId}] {message.Author.Username}: {message.Content}", message.ChannelId,
            message.Author.Username, message.Content);

        var matches = BranikRegex().Matches(message.Content);

        if (matches.Count is 0)
            return;

        var chatMessage = new StringBuilder();
        var amount = 0.0;

        foreach (Match match in matches)
        {
            var quoteValue = match.Groups[0].Value;
            chatMessage.AppendLine($"> {quoteValue}");

            if (match.Groups[nameof(PriceGroup.Ones)].Success)
            {
                amount = double.Parse(match.Groups[nameof(PriceGroup.Ones)].Value);
            }
            else if (match.Groups[nameof(PriceGroup.Thousands)].Success)
            {
                amount = double.Parse(match.Groups[nameof(PriceGroup.Thousands)].Value) * 1000;
            }
            else if (match.Groups[nameof(PriceGroup.Millions)].Success)
            {
                amount = double.Parse(match.Groups[nameof(PriceGroup.Millions)].Value) * 1_000_000;
            }
            
            var branikCount = (int)(amount / BranikPrice);

            if (branikCount <= 0)
            {
                chatMessage.AppendLine("\nJe mi to lito, ale to neni ani na jednu dvoulitrovku Branika ve sleve.\n");
                continue;
            }

            var parcelCount = branikCount / 6;
            var palletsCount = parcelCount / 100;
            var trucksCount = palletsCount / 34;
            var bottlesWord = GetBottleWordFormat(branikCount);
            var parcelsWord = GetParcelssWordFormat(parcelCount);
            var palletsWord = GetPalletsWordFormat(palletsCount);
            var trucksWord = GetTrucksWordFormat(palletsCount);
            
            switch (branikCount)
            {
                case < 100:
                    chatMessage.AppendLine($"\nTo by stacilo na {branikCount} {bottlesWord} Branicka ve sleve!\n");
                    break;
                case < 1000:
                {
                    chatMessage.AppendLine($"\nTo by stacilo na {parcelCount} {parcelsWord} {bottlesWord} Branicka ve sleve!\n");
                    break;
                }
                case < 100_000:
                {
                    chatMessage.AppendLine($"\nTo by stacilo na vic jak {palletsCount} euro{palletsWord} ({parcelCount} {parcelsWord}) {bottlesWord} Branicka ve sleve!\n");
                    break;
                }
                case >= 100_000:
                {
                    chatMessage.AppendLine($"\nTo by stacilo na {trucksCount} {trucksWord} ({palletsCount} {palletsWord}) {bottlesWord} Branicka ve sleve!\n");
                    break;
                }
            }
        }

        await _gatewayClient.Rest.SendMessageAsync(message.ChannelId, chatMessage.ToString());
    }

    private static string GetBottleWordFormat(int branikCount)
    {
        return branikCount switch
        {
            1 => "dvoulitrovku",
            >= 2 and <= 4 => "dvoulitrovky",
            _ => "dvoulitrovek"
        };
    }
    
    private static string GetParcelssWordFormat(int parcelsCount)
    {
        return parcelsCount switch
        {
            1 => "balik",
            >= 2 and <= 4 => "baliky",
            _ => "baliku"
        };
    }
    
    private static string GetPalletsWordFormat(int palletsCount)
    {
        return palletsCount switch
        {
            1 => "paletu",
            >= 2 and <= 4 => "palety",
            _ => "palet"
        };
    }
    
    private static string GetTrucksWordFormat(int trucksCount)
    {
        return trucksCount switch
        {
            1 => "kamion",
            >= 2 and <= 4 => "kamiony",
            _ => "kamionu"
        };
    }

    [GeneratedRegex(Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, "cs-CZ")]
    private static partial Regex BranikRegex();
}
