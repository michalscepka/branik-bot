using BranikBot.Infrastructure.Extensions;
using Serilog;

var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Logging.Environments.Production;
Log.Logger = Logging.LoggerConfigurationExtensions.ConfigureMinimalLogging(environmentName);

try
{
    Log.Information("Starting web host on {env} environment.", environmentName);
    var builder = WebApplication.CreateBuilder(args);

    Log.Debug("Use Serilog");
    builder.Host.UseSerilog(
        (context, _, loggerConfiguration) =>
        {
            Logging.LoggerConfigurationExtensions.SetupLogger(context.Configuration, loggerConfiguration);
        }, preserveStaticLogger: true);

    Log.Debug("Adding services");
    builder.Services.AddInfrastructure();

    var app = builder.Build();

    app.Run();
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
