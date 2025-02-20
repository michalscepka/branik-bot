using BranikBot.Infrastructure.Configuration;
using BranikBot.Infrastructure.Services;
using BranikBot.Infrastructure.Services.Abstractions;
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
        
        services.AddOptions<CashingConfiguration>()
            .BindConfiguration(CashingConfiguration.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDiscordGateway((opt, sp) =>
        {
            opt.Token = sp.GetRequiredService<IOptions<DiscordConfiguration>>().Value.Token;
            opt.Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent;
        });

        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddSingleton<IMessageHandler, BranikMessageHandler>();
        services.AddSingleton<IWebScrapingService, BranikPriceService>();
        services.AddHostedService<DiscordService>();
        
        return services;
    }
}
