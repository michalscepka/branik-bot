using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Services.ApplicationCommands;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDiscordGateway()
    .AddApplicationCommands<ApplicationCommandInteraction, ApplicationCommandContext>();

var app = builder.Build();

app.AddModules(typeof(Program).Assembly);

app.UseGatewayEventHandlers();

app.Run();
