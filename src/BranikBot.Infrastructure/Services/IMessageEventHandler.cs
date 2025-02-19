using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services;

public interface IMessageEventHandler
{
    ValueTask HandleMessage(Message message);
}
