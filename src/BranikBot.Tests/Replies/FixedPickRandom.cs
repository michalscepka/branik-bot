namespace BranikBot.Tests.Replies;

// Always picks the item at the given index, so a test knows which phrasing it gets.
public sealed class FixedPickRandom(int index) : Random
{
    public static readonly FixedPickRandom First = new(0);

    public override int Next(int maxValue) => index;
}
