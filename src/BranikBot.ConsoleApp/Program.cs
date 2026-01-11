using BranikBot.Infrastructure.Extensions;
using BranikBot.ConsoleApp.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Globalization;

try
{
    var cultureInfo = new CultureInfo("cs-CZ");
    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

    var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Logging.Environments.Production;
    Log.Logger = Logging.LoggerConfigurationHelper.ConfigureMinimalLogging(environmentName);

    Log.Information("Starting web host on {env} environment.", environmentName);
    var builder = Host.CreateDefaultBuilder(args);

    Log.Debug("Use Serilog");
    builder.ConfigureSerilog();

    Log.Debug("Adding services");
    builder.ConfigureServices(services =>
    {
        services.AddInfrastructure();
    });

    await builder.Build().RunAsync();
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
