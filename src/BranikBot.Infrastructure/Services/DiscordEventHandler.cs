using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public class DiscordEventHandler : BackgroundService
{
    private readonly GatewayClient _gatewayClient;
    private readonly ILogger<DiscordEventHandler> _logger;
    private readonly IMessageEventHandler _branikMessageEventHandler;

    public DiscordEventHandler(GatewayClient gatewayClient, IMessageEventHandler branikMessageEventHandler, ILogger<DiscordEventHandler> logger)
    {
        _gatewayClient = gatewayClient;
        _branikMessageEventHandler = branikMessageEventHandler;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DiscordEventHandler is starting...");
        _gatewayClient.MessageCreate += _branikMessageEventHandler.HandleMessage;
        return Task.CompletedTask;
    }
}
