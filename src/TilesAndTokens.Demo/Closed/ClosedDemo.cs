using TilesAndTokens.Domain;

namespace TilesAndTokens.Demo.Closed;

/// <summary>Post 3: placement rules and victory conditions as closed hierarchies.</summary>
internal sealed class ClosedDemo : IDemo
{
    private const int Seed = 2026;
    private const int Row = 3;

    private static readonly PlayerId Alice = new("alice");
    private static readonly PlayerId Bob = new("bob");

    public string Command => "closed";

    public string Title => "Post 3: Closed hierarchies";

    public void Run()
    {
        Narrator.Step("The hierarchies");
        Narrator.Code("public closed record PlacementRule { public abstract PlacementResult Check(Board board, Tile candidate); }");
        Narrator.Code("public closed record VictoryCondition { public abstract string Name { get; } }");
        Narrator.Say("Every subtype lives in TilesAndTokens.Domain. Another assembly can't add one (CS9382),");
        Narrator.Say("so a switch over them can be exhaustive with no discard arm.");

        ValidatePlacements();
        PlayToFirstToEdge();
    }

    private static void ValidatePlacements()
    {
        Narrator.Step("One placement per rule");
        var board = Board.Empty
            .Place(new Tile(new Position(2, 2), Terrain.Mountain, Alice))
            .Place(new Tile(new Position(5, 5), Terrain.Forest, Bob));

        Narrator.Say($"Rules, in order: {string.Join(" -> ", PlacementValidator.Rules.Select(r => r.GetType().Name))}");

        Validate(board, new Tile(new Position(8, 2), Terrain.Plains, Alice), "off the board");
        Validate(board, new Tile(new Position(5, 5), Terrain.Plains, Alice), "on bob's tile");
        Validate(board, new Tile(new Position(6, 6), Terrain.Plains, Alice), "far from alice's tiles");
        Validate(board, new Tile(new Position(2, 3), Terrain.Water, Alice), "water next to mountain");
        Validate(board, new Tile(new Position(2, 3), Terrain.Forest, Alice), "forest next to mountain");
    }

    private static void Validate(Board board, Tile candidate, string description)
    {
        Narrator.Code($"{candidate.Terrain} at {candidate.Position} ({description})");

        switch (PlacementValidator.Validate(board, candidate))
        {
            case Placed placed:
                Narrator.Outcome($"Placed (+{placed.PointsAwarded} pt)");
                break;
            case Rejected rejected:
                Narrator.Problem($"Rejected by {rejected.Rule}: {rejected.Reason}");
                break;
            case Blocked blocked:
                Narrator.Problem($"Blocked: {blocked.Position} is occupied");
                break;
        }
    }

    private static void PlayToFirstToEdge()
    {
        var victory = new FirstToEdge();
        Narrator.Step($"A short game: {victory.Name}");
        Narrator.Say($"alice holds {Board.Size} Plains tiles and builds across row {Row}. bob just rolls dice.");

        var engine = new TurnEngine(new SeededDiceRoller(Seed));
        var state = RiggedGame(victory);

        for (var x = 0; x < Board.Size; x++)
        {
            var aliceTurn = engine.Apply(state, new PlayCard(Alice, PlainsCard(x).Id, new Position(x, Row)));
            if (Advance(aliceTurn, ref state))
            {
                break;
            }

            Advance(engine.Apply(state, new RollDice(Bob)), ref state);
        }

        PrintBoard(state.Board);
    }

    /// <returns>True if the game is over.</returns>
    private static bool Advance(TurnOutcome outcome, ref GameState state)
    {
        switch (outcome)
        {
            case ActionApplied applied:
                Narrator.Outcome(applied.Summary);
                state = applied.State;
                return false;

            case ActionInvalid invalid:
                Narrator.Problem($"Invalid: {invalid.Reason}");
                return false;

            case GameOver over:
                Narrator.Outcome(over.Summary);
                Narrator.Outcome($"Game over: {over.Winner} wins ({over.State.Victory.Name}).");
                state = over.State;
                return true;
        }

        throw new InvalidOperationException("Empty TurnOutcome.");
    }

    private static GameState RiggedGame(VictoryCondition victory) => new(
        Board.Empty,
        Players:
        [
            new Player(Alice, [.. Enumerable.Range(0, Board.Size).Select(x => (Card)PlainsCard(x))]),
            new Player(Bob, []),
        ],
        Deck: [],
        victory,
        TurnLog: [],
        Round: 1,
        CurrentPlayer: Alice);

    private static TileCard PlainsCard(int x) => new(new CardId($"plains-{x + 1}"), Terrain.Plains);

    private static void PrintBoard(Board board)
    {
        Narrator.Say("");
        for (var y = 0; y < Board.Size; y++)
        {
            var cells = Enumerable.Range(0, Board.Size)
                .Select(x => board.TileAt(new Position(x, y)) is { } tile ? char.ToUpperInvariant(tile.Owner.Value[0]) : '.');
            Narrator.Say($"    {string.Join(' ', cells)}");
        }
    }
}
