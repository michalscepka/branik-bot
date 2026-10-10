using BranikBot.Configuration;
using BranikBot.Pricing.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Pricing;

public class ExchangeRateSamplingBackgroundService(
    IExchangeRateProvider exchangeRateProvider,
    LatestExchangeRate latestExchangeRate,
    TimeProvider timeProvider,
    IOptions<ExchangeRateConfiguration> configuration,
    ILogger<ExchangeRateSamplingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(configuration.Value.SampleInterval, timeProvider);

        do
        {
            await SampleAsync();
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task SampleAsync()
    {
        try
        {
            var rates = await exchangeRateProvider.GetExchangeRatesAsync();
            if (rates is null)
                return;

            foreach (var (currency, rate) in rates)
            {
                if (rate > 0)
                    latestExchangeRate.Set(currency, rate);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sample the exchange rate");
        }
    }
}
