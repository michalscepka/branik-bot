namespace BranikBot.Pricing;

public record PricePeriod(decimal Price, DateTimeOffset FirstSeenAt, DateTimeOffset LastSeenAt);
