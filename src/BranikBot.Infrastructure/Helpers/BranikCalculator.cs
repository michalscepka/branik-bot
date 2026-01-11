namespace BranikBot.Infrastructure.Helpers;

public static class BranikCalculator
{
    private const int ParcelSize = 6;
    private const int PalletSize = 100;
    private const int TruckSize = 34;

    public static (int BranikCount, int ParcelCount, int PalletsCount) CalculateAmounts(decimal amount, decimal branikPrice)
    {
        var branikCount = (int)(amount / branikPrice);
        var parcelCount = branikCount / ParcelSize;
        var palletsCount = parcelCount / PalletSize;
        return (branikCount, parcelCount, palletsCount);
    }
}
