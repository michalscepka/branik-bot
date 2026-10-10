using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BranikBot.Logging.Extensions;

internal static class HostBuilderExtensions
{
    public static IHostBuilder ConfigureSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureLogging((builder, logging) =>
        {
            logging.ClearProviders();
            LoggerConfigurationHelper.SetupLoggerConfiguration(builder.Configuration);
        }).UseSerilog();

        return hostBuilder;
    }
}
