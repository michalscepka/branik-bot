namespace BranikBot.Application.Services;

public interface IPriceService
{
    ValueTask<decimal> GetPriceAsync();
}
