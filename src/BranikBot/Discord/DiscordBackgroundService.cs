using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace BranikBot.Discord;

public class DiscordBackgroundService(
    GatewayClient gatewayClient,
    MessageHandler messageHandler,
    BotStatus botStatus,
    ILogger<DiscordBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Discord service is starting");
        gatewayClient.MessageCreate += messageHandler.ProcessIncomingMessage;
        gatewayClient.Ready += RefreshStatus;

        // Ready fires once after login and may come before the line above subscribed, so set the status now as well.
        await botStatus.RefreshAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Discord service is stopping");
        gatewayClient.MessageCreate -= messageHandler.ProcessIncomingMessage;
        gatewayClient.Ready -= RefreshStatus;
        await base.StopAsync(cancellationToken);
    }

    private async ValueTask RefreshStatus(ReadyEventArgs args) => await botStatus.RefreshAsync();
}
