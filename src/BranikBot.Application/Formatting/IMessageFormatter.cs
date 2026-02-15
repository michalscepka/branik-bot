using BranikBot.Domain.Models;

namespace BranikBot.Application.Formatting;

public interface IMessageFormatter
{
    ValueTask<string> FormatMessageAsync(IEnumerable<UserInput> userInputs, decimal marketPrice);
}
