using TilesAndTokens.Domain;

namespace TilesAndTokens.Persistence;

/// <summary>Plays many short, deterministic games with the real engine, for seeding and tests.</summary>
public static class GameGenerator
{
    public const int PlayerPoolSize = 200;

    public static IEnumerable<GameState> Generate(int count, int seed)
    {
        var random = new Random(seed);

        for (var i = 0; i < count; i++)
        {
            var players = PickPlayers(random);
            VictoryCondition victory = (i % 3) switch
            {
                0 => new MostPoints(Rounds: 10),
                1 => new FirstToEdge(),
                _ => new LastPlayerStanding(),
            };

            yield return Play(
                GameSetup.NewGame(players, StandardDeck.Create(), new SeededDeckShuffler(random.Next()), victory),
                new TurnEngine(new SeededDiceRoller(random.Next())),
                random,
                turns: random.Next(4, 30));
        }
    }

    private static GameState Play(GameState state, TurnEngine engine, Random random, int turns)
    {
        for (var turn = 0; turn < turns; turn++)
        {
            switch (engine.Apply(state, ChooseAction(state, random)))
            {
                case ActionApplied applied:
                    state = applied.State;
                    break;

                case GameOver over:
                    return over.State;

                case ActionInvalid invalid:
                    throw new InvalidOperationException($"The generator chose an invalid action: {invalid.Reason}");
            }
        }

        return state;
    }

    /// <summary>
    /// Like <see cref="SimpleStrategy"/>, but tiles go to a random legal cell. SimpleStrategy always
    /// starts in the corner, which would put almost every player "on an edge".
    /// </summary>
    private static GameAction ChooseAction(GameState state, Random random)
    {
        var player = state.GetPlayer(state.CurrentPlayer);

        foreach (var card in player.Hand)
        {
            switch (card)
            {
                case BonusPointsCard bonus:
                    return new PlayCard(player.Id, bonus.Id);

                case TileCard tile when LegalPositions(state.Board, tile.Terrain, player.Id) is { Count: > 0 } legal:
                    return new PlayCard(player.Id, tile.Id, legal[random.Next(legal.Count)]);
            }
        }

        return new RollDice(player.Id);
    }

    private static List<Position> LegalPositions(Board board, Terrain terrain, PlayerId owner) =>
    [
        .. from y in Enumerable.Range(0, Board.Size)
           from x in Enumerable.Range(0, Board.Size)
           let position = new Position(x, y)
           where PlacementValidator.Validate(board, new Tile(position, terrain, owner)) is Placed
           select position,
    ];

    private static List<PlayerId> PickPlayers(Random random)
    {
        var count = random.Next(GameSetup.MinPlayers, GameSetup.MaxPlayers + 1);

        return [.. Enumerable.Range(1, PlayerPoolSize)
            .OrderBy(_ => random.Next())
            .Take(count)
            .Select(n => new PlayerId($"player-{n:000}"))];
    }
}
