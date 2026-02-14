using BranikBot.Domain.Enums;

namespace BranikBot.Application.Services;

public interface IExchangeRateService
{
    ValueTask<decimal> GetExchangeRateAsync(Currency currency);
}
