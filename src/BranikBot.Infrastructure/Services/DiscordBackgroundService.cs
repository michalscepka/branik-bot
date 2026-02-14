using BranikBot.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public class DiscordBackgroundService(
    GatewayClient gatewayClient,
    IMessageHandler messageHandler,
    ILogger<DiscordBackgroundService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Discord service is starting");
        gatewayClient.MessageCreate += messageHandler.ProcessIncomingMessage;
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Discord service is stopping");
        gatewayClient.MessageCreate -= messageHandler.ProcessIncomingMessage;
        await base.StopAsync(cancellationToken);
    }
}
