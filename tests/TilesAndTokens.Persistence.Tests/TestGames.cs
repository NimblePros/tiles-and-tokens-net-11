using TilesAndTokens.Domain;

namespace TilesAndTokens.Persistence.Tests;

internal static class TestGames
{
    /// <summary>A unique player id per call, so tests sharing one database don't see each other's rows.</summary>
    public static PlayerId UniquePlayer(string name) => new($"{name}-{Guid.NewGuid():N}");

    /// <summary>A hand-built game whose board holds exactly the given tiles.</summary>
    public static GameState WithTiles(PlayerId leader, VictoryCondition victory, params Tile[] tiles)
    {
        var other = UniquePlayer("other");

        return new GameState(
            new Board(tiles),
            Players: [new Player(leader, [], Score: 10), new Player(other, [], Score: 3)],
            Deck: [],
            victory,
            TurnLog: [new RollDice(leader), new Pass(other), new PlayCard(leader, new CardId("plains-1"), new Position(0, 0))],
            Round: 2,
            CurrentPlayer: other);
    }

    public static Tile TileAt(int x, int y, PlayerId owner) => new(new Position(x, y), Terrain.Plains, owner);
}
