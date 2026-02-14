using BranikBot.Domain.Enums;

namespace BranikBot.Application.Services;

public interface IExchangeRateService
{
    Task<decimal> GetExchangeRateAsync(Currency currency);
}
