using BranikBot.Domain.Models;

namespace BranikBot.Application.Services;

public interface IMessageFormatter
{
    ValueTask<string> FormatMessageAsync(IEnumerable<UserInput> userInputs, decimal marketPrice);
}
