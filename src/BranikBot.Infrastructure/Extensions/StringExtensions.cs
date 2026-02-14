namespace BranikBot.Infrastructure.Extensions;

public static class StringExtensions
{
    extension(int count)
    {
        public string GetBottleWord() => count switch
        {
            1 => "dvoulitrovku",
            >= 2 and <= 4 => "dvoulitrovky",
            _ => "dvoulitrovek"
        };

        public string GetParcelWord() => count switch
        {
            1 => "balik",
            >= 2 and <= 4 => "baliky",
            _ => "baliku"
        };

        public string GetPalletWord() => count switch
        {
            1 => "europaletu",
            >= 2 and <= 4 => "europalety",
            _ => "europalet"
        };
    }
}
