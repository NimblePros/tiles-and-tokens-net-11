namespace TilesAndTokens.Domain;

public interface IDeckShuffler
{
    IReadOnlyList<Card> Shuffle(IEnumerable<Card> deck);
}
