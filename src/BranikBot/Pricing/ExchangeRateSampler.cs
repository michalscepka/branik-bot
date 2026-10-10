using BranikBot.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Pricing;

public class ExchangeRateSampler(
    IExchangeRateProvider exchangeRateProvider,
    ExchangeRateStore exchangeRateStore,
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
            if (rate <= 0)
                return;

            exchangeRateStore.Record(Currency.Eur, rate, timeProvider.GetUtcNow());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store the exchange rate sample");
        }
    }
}
