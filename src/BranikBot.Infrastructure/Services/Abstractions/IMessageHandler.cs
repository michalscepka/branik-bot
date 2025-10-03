using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services.Abstractions;

public interface IMessageHandler
{
    ValueTask ProcessIncomingMessage(Message message);
}
