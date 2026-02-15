using BranikBot.Domain.Enums;

namespace BranikBot.Application.Providers;

public interface IExchangeRateProvider
{
    ValueTask<decimal> GetExchangeRateAsync(Currency currency);
}
