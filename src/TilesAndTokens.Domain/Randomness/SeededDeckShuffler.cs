namespace TilesAndTokens.Domain;

public sealed class SeededDeckShuffler(int seed) : IDeckShuffler
{
    private readonly Random _random = new(seed);

    public IReadOnlyList<Card> Shuffle(IEnumerable<Card> deck)
    {
        var cards = deck.ToArray();
        _random.Shuffle(cards);
        return cards;
    }
}
