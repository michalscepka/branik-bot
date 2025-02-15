using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public class DiscordEventHandler : BackgroundService
{
    private readonly GatewayClient _gatewayClient;
    private readonly ILogger<DiscordEventHandler> _logger;
    
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

        _logger.LogInformation("[{message.ChannelId}] {message.Author.Username}: {message.Content}", message.ChannelId, message.Author.Username, message.Content);

        if (message.Content == "ping")
            await _gatewayClient.Rest.SendMessageAsync(message.ChannelId,"pong");
    }
}
