namespace TilesAndTokens.Domain.Tests;

/// <summary>Hand-built game states so each test controls exactly what's in play.</summary>
internal static class TestGame
{
    public static readonly PlayerId Alice = new("alice");
    public static readonly PlayerId Bob = new("bob");

    public static readonly TileCard Forest = new(new CardId("forest-1"), Terrain.Forest);
    public static readonly TileCard Water = new(new CardId("water-1"), Terrain.Water);
    public static readonly BonusPointsCard Bonus = new(new CardId("bonus-1"), 3);
    public static readonly TileCard DeckCard = new(new CardId("deck-1"), Terrain.Plains);

    /// <remarks>Defaults to a victory condition that can't trigger during a short test.</remarks>
    public static GameState New(Board? board = null, VictoryCondition? victory = null) => new(
        board ?? Board.Empty,
        Players:
        [
            new Player(Alice, [Forest, Water, Bonus]),
            new Player(Bob, []),
        ],
        Deck: [DeckCard],
        victory ?? new MostPoints(Rounds: 100),
        TurnLog: [],
        Round: 1,
        CurrentPlayer: Alice);
}
