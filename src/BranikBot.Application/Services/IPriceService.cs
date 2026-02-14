namespace BranikBot.Application.Services;

public interface IPriceService
{
    Task<decimal> GetPriceAsync();
}
