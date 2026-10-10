namespace BranikBot.Replies;

public class ReplyChance(double chance, Random random)
{
    public bool Roll() => random.NextDouble() < chance;
}
