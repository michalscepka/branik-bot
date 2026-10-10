using BranikBot.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranikBot.Pricing;

public class PriceSampler(
    IMarketPriceProvider marketPriceProvider,
    PriceHistoryStore priceHistoryStore,
    TimeProvider timeProvider,
    IOptions<MarketConfiguration> configuration,
    ILogger<PriceSampler> logger) : BackgroundService
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
            var price = await marketPriceProvider.GetPriceAsync();
            if (price is null)
                return;

            priceHistoryStore.Record(price.Value, timeProvider.GetUtcNow());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store the market price sample");
        }
    }
}
