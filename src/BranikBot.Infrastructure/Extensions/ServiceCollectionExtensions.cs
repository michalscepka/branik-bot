using BranikBot.Infrastructure.Configuration;
using BranikBot.Infrastructure.Services;
using BranikBot.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace BranikBot.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
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

        services.AddDiscordGateway((opt, sp) =>
        {
            opt.Token = sp.GetRequiredService<IOptions<DiscordConfiguration>>().Value.BotToken;
            opt.Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent;
        });

        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddSingleton<IMessageHandler, MessageHandler>();
        services.AddSingleton<IPriceService, AkcniCenyService>();
        services.AddSingleton<IExchangeRateService, CnbExchangeRateService>();
        services.AddSingleton<IMessageFormatter, MessageFormatter>();
        services.AddHostedService<DiscordService>();

        return services;
    }
}
