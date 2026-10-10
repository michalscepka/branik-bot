using BranikBot.Configuration;
using BranikBot.Discord;
using BranikBot.Persistence;
using BranikBot.Pricing;
using BranikBot.Pricing.Abstractions;
using BranikBot.Replies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace BranikBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBranikBot(this IServiceCollection services)
    {
        services.AddOptions<DiscordConfiguration>()
            .BindConfiguration(DiscordConfiguration.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<MarketConfiguration>()
            .BindConfiguration(MarketConfiguration.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ExchangeRateConfiguration>()
            .BindConfiguration(ExchangeRateConfiguration.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseConfiguration>()
            .BindConfiguration(DatabaseConfiguration.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDiscordGateway((opt, sp) =>
        {
            opt.Token = sp.GetRequiredService<IOptions<DiscordConfiguration>>().Value.BotToken;
            opt.Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent;
        });

        services.AddHttpClient();
        services.AddHttpClient<IMarketPriceProvider, AkcniCenyProvider>();
        services.AddHttpClient<IExchangeRateProvider, CnbExchangeRateProvider>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(sp =>
            new SqliteDatabase(sp.GetRequiredService<IOptions<DatabaseConfiguration>>().Value.Path));
        services.AddSingleton<PriceHistoryStore>();
        services.AddSingleton<LatestExchangeRate>();
        services.AddHostedService<PriceSamplingBackgroundService>();
        services.AddHostedService<ExchangeRateSamplingBackgroundService>();

        services.AddSingleton(sp => new ChannelCooldown(
            sp.GetRequiredService<IOptions<DiscordConfiguration>>().Value.ChannelCooldown,
            sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<EuroChampionMessageReader>();
        services.AddSingleton<ReplyBuilder>();
        services.AddSingleton<MessageHandler>();
        services.AddHostedService<DiscordBackgroundService>();

        return services;
    }
}
