using NetCord.Gateway;

namespace BranikBot.Infrastructure.Services.Abstractions;

public interface IMessageHandler
{
    ValueTask HandleMessage(Message message);
}
