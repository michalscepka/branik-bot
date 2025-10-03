using BranikBot.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public class DiscordService : BackgroundService
{
    private readonly GatewayClient _gatewayClient;
    private readonly ILogger<DiscordService> _logger;
    private readonly IMessageHandler _messageHandler;

    public DiscordService(GatewayClient gatewayClient, IMessageHandler messageHandler, ILogger<DiscordService> logger)
    {
        _gatewayClient = gatewayClient;
        _messageHandler = messageHandler;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Discord service is starting");
        _gatewayClient.MessageCreate += _messageHandler.ProcessIncomingMessage;
        return Task.CompletedTask;
    }
}
