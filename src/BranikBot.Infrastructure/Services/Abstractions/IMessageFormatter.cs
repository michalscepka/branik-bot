using BranikBot.Infrastructure.Models;

namespace BranikBot.Infrastructure.Services.Abstractions;

public interface IMessageFormatter
{
    Task<string> FormatMessageAsync(IEnumerable<ParsedPrice> prices, decimal marketPrice);
}
