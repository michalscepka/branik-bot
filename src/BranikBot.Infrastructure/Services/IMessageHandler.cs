using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public interface IMessageHandler
{
    ValueTask ProcessIncomingMessage(Message message);
}
