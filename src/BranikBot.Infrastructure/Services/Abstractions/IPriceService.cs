namespace BranikBot.Infrastructure.Services.Abstractions;

public interface IPriceService
{
    Task<decimal> GetPriceAsync();
}
