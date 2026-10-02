namespace TilesAndTokens.Legacy.Tests;

internal static class GameFixture
{
    public static readonly Card ForestCard = new("forest-1", "Forest Tile", Terrain: Terrain.Forest);

    public static readonly Card BonusCard = new("bonus-1", "Bonus Points", BonusPoints: 3);

    public static LegacyGame NewGame()
    {
        var alice = new LegacyPlayer("alice");
        alice.Hand.AddRange([ForestCard, BonusCard]);

        return new LegacyGame([alice, new LegacyPlayer("bob")], deck: []);
    }
}
