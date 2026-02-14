using BranikBot.Domain.Models;

namespace BranikBot.Application.Services;

public interface IMessageFormatter
{
    Task<string> FormatMessageAsync(IEnumerable<ParsedPrice> prices, decimal marketPrice);
}
