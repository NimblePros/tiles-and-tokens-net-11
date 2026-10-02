namespace TilesAndTokens.Domain;

public static class StandardDeck
{
    public const int TileCardsPerTerrain = 6;
    public const int BonusCards = 4;
    public const int BonusPoints = 3;

    public static IReadOnlyList<Card> Create()
    {
        List<Card> cards = [];

        foreach (var terrain in Enum.GetValues<Terrain>())
        {
            for (var i = 1; i <= TileCardsPerTerrain; i++)
            {
                cards.Add(new TileCard(new CardId($"{terrain.ToString().ToLowerInvariant()}-{i}"), terrain));
            }
        }

        for (var i = 1; i <= BonusCards; i++)
        {
            cards.Add(new BonusPointsCard(new CardId($"bonus-{i}"), BonusPoints));
        }

        return cards;
    }
}
