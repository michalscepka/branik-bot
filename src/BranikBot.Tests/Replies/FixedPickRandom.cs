namespace BranikBot.Tests.Replies;

// Returns the given picks in order and then keeps returning the last one,
// so a test knows which phrasing and which extra fact it gets.
public sealed class FixedPickRandom(params int[] picks) : Random
{
    private int _position;

    // The first phrasing and the first extra fact.
    public static FixedPickRandom First => new(0);

    public override int Next(int maxValue)
    {
        var pick = picks[Math.Min(_position, picks.Length - 1)];
        _position++;
        return pick;
    }
}
