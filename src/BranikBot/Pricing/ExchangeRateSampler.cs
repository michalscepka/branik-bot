using BranikBot.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Pricing;

public class ExchangeRateSampler(
    IExchangeRateProvider exchangeRateProvider,
    LatestExchangeRate latestExchangeRate,
    TimeProvider timeProvider,
    IOptions<ExchangeRateConfiguration> configuration,
    ILogger<ExchangeRateSampler> logger) : BackgroundService
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
            var rate = await exchangeRateProvider.GetExchangeRateAsync(Currency.Eur);
            if (rate is not > 0)
                return;

            latestExchangeRate.Set(Currency.Eur, rate.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sample the exchange rate");
        }
    }
}
