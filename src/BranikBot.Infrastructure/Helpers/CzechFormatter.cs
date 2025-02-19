namespace BranikBot.Infrastructure.Helpers;

public static class CzechFormatter
{
    public static string GetBottleWord(this int count) => count switch
    {
        1 => "dvoulitrovku",
        >= 2 and <= 4 => "dvoulitrovky",
        _ => "dvoulitrovek"
    };

    public static string GetParcelWord(this int count) => count switch
    {
        1 => "balik",
        >= 2 and <= 4 => "baliky",
        _ => "baliku"
    };

    public static string GetPalletWord(this int count) => count switch
    {
        1 => "europaletu",
        >= 2 and <= 4 => "europalety",
        _ => "europalet"
    };

    public static string GetTruckWord(this int count) => count switch
    {
        1 => "kamion",
        >= 2 and <= 4 => "kamiony",
        _ => "kamionu"
    };
}
