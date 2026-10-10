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
using NetCord.Hosting.Services.ApplicationCommands;

namespace BranikBot.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly TimeSpan PooledConnectionLifetime = TimeSpan.FromMinutes(2);

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
        services.AddApplicationCommands();

        ConfigureLongLivedClient(services.AddHttpClient<IMarketPriceProvider, AkcniCenyProvider>());
        ConfigureLongLivedClient(services.AddHttpClient<IExchangeRateProvider, CnbExchangeRateProvider>());

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(sp =>
            new SqliteDatabase(sp.GetRequiredService<IOptions<DatabaseConfiguration>>().Value.Path));
        services.AddSingleton<PriceHistoryStore>();
        services.AddSingleton<TrustedBotStore>();
        services.AddSingleton<LatestExchangeRate>();
        services.AddHostedService<PriceSamplingBackgroundService>();
        services.AddHostedService<ExchangeRateSamplingBackgroundService>();

        services.AddSingleton(sp => new ReplyCooldown(
            sp.GetRequiredService<IOptions<DiscordConfiguration>>().Value.ReplyCooldown,
            sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton(sp => new ReplyChance(
            sp.GetRequiredService<IOptions<DiscordConfiguration>>().Value.ReplyChance,
            Random.Shared));
        services.AddSingleton<BotMessageReader>();
        services.AddSingleton<ReplyBuilder>();
        services.AddSingleton<MessageHandler>();
        services.AddHostedService<DiscordBackgroundService>();

        return services;
    }

    // The providers are held by background services for the whole process, so the factory never
    // rotates their handler. Recycling pooled connections keeps DNS changes visible instead.
    private static void ConfigureLongLivedClient(IHttpClientBuilder builder)
    {
        builder
            .UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = PooledConnectionLifetime)
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
    }
}
