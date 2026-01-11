using BranikBot.Infrastructure.Enums;

namespace BranikBot.Infrastructure.Services.Abstractions;

public interface IExchangeRateService
{
    Task<decimal> GetExchangeRateAsync(Currency currency);
}
