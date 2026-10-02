namespace TilesAndTokens.Domain;

public sealed record Player(PlayerId Id, IReadOnlyList<Card> Hand, int Score = 0, int ConsecutivePasses = 0)
{
    public const int MaxConsecutivePasses = 3;

    public bool IsEliminated => ConsecutivePasses >= MaxConsecutivePasses;

    /// <remarks>
    /// Not <c>Hand.FirstOrDefault(...)</c>: Card is a struct, so "not found" would be an
    /// empty union rather than null.
    /// </remarks>
    public bool TryGetCard(CardId id, out Card card)
    {
        foreach (var candidate in Hand)
        {
            if (candidate.Id == id)
            {
                card = candidate;
                return true;
            }
        }

        card = default;
        return false;
    }

    public Player WithoutCard(CardId id) => this with { Hand = [.. Hand.Where(c => c.Id != id)] };
}
