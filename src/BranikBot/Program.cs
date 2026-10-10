using System.Globalization;
using BranikBot;
using BranikBot.Extensions;
using BranikBot.Logging.Extensions;
using Microsoft.Extensions.Hosting;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using Serilog;

try
{
    var cultureInfo = new CultureInfo("cs-CZ");
    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

    var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Production;
    Log.Logger = LoggerConfigurationHelper.ConfigureMinimalLogging(environmentName);

    Log.Information("Starting web host on {env} environment.", environmentName);
    var builder = Host.CreateDefaultBuilder(args);

    Log.Debug("Use Serilog");
    builder.ConfigureSerilog();

    Log.Debug("Adding services");
    builder.ConfigureServices(services =>
    {
        services.AddBranikBot();
    });

    var host = builder.Build();
    host.AddModules(typeof(Program).Assembly);
    host.UseGatewayEventHandlers();

    await host.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down application");
    await Log.CloseAndFlushAsync();
}
