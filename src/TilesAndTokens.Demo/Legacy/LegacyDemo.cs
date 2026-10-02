using TilesAndTokens.Legacy;
using TilesAndTokens.Legacy.BaseClass;
using TilesAndTokens.Legacy.NullableWrapper;
using TilesAndTokens.Legacy.StringFlags;

namespace TilesAndTokens.Demo.Legacy;

/// <summary>Post 1: three common ways to model a turn, and how each one fails.</summary>
internal sealed class LegacyDemo : IDemo
{
    private static readonly Card ForestCard = new("forest-1", "Forest Tile", Terrain: Terrain.Forest);

    public string Command => "legacy";

    public string Title => "Post 1: The \"before\" picture";

    public void Run()
    {
        StringFlags();
        NullableWrapper();
        BaseClassAndCasting();
        ResultWrapper();

        Narrator.Step("Coming up");
        Narrator.Say("Post 2 replaces all of this with unions, so the compiler catches these mistakes.");
    }

    private static void StringFlags()
    {
        Narrator.Step("1. String flags");
        var game = NewGame();

        Narrator.Code("new StringTurn { ActionType = \"RollDice\", DiceTotal = 7 }");
        Narrator.Outcome(StringTurnProcessor.Apply(game, new StringTurn { PlayerId = "alice", ActionType = "RollDice", DiceTotal = 7 }));

        Narrator.Code("new StringTurn { ActionType = \"Rolldice\", DiceTotal = 7 }   // typo");
        var message = StringTurnProcessor.Apply(game, new StringTurn { PlayerId = "alice", ActionType = "Rolldice", DiceTotal = 7 });
        Narrator.Problem($"Compiled fine. Result: \"{message}\". alice's score is still {game.Player("alice").Score}. The turn vanished.");
    }

    private static void NullableWrapper()
    {
        Narrator.Step("2. Null-heavy wrapper");
        var game = NewGame();

        Narrator.Code("new TurnAction { Roll = new DiceRoll(3, 4), Card = forestCard, Target = (0,0) }");
        var invalid = new TurnAction { PlayerId = "alice", Roll = new DiceRoll(3, 4), Card = ForestCard, Target = new Position(0, 0) };
        Narrator.Outcome(TurnActionProcessor.Apply(game, invalid));
        Narrator.Problem($"Rolling AND playing a card is illegal, but it compiled. The card was silently ignored ({game.Board.Tiles.Count} tiles on the board).");
    }

    private static void BaseClassAndCasting()
    {
        Narrator.Step("3. Base class + casting");
        var game = NewGame();

        Narrator.Code("LegacyActionProcessor.Apply(game, new PassAction(\"bob\"))");
        Narrator.Outcome(LegacyActionProcessor.Apply(game, new PassAction("bob")));

        Narrator.Code("LegacyActionProcessor.Apply(game, new TradeAction(...))   // added later");
        try
        {
            LegacyActionProcessor.Apply(game, new TradeAction("alice", "bob", "forest-1", "water-1"));
        }
        catch (NotSupportedException ex)
        {
            Narrator.Problem($"Compiled fine, then at runtime: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void ResultWrapper()
    {
        Narrator.Step("4. Hand-rolled Result<T>");
        var board = new LegacyBoard();
        board.Place(new Tile(new Position(3, 3), Terrain.Plains, "bob"));

        Narrator.Code("var result = LegacyPlacementValidator.Validate(board, tileOnOccupiedCell);");
        var result = LegacyPlacementValidator.Validate(board, new Tile(new Position(3, 3), Terrain.Forest, "alice"));
        Narrator.Outcome($"IsSuccess = {result.IsSuccess}, Error = \"{result.Error}\"");

        Narrator.Code("result.Value   // nobody checked IsSuccess");
        try
        {
            _ = result.Value;
        }
        catch (InvalidOperationException ex)
        {
            Narrator.Problem($"Compiled fine, then at runtime: {ex.Message}");
        }
    }

    private static LegacyGame NewGame()
    {
        var alice = new LegacyPlayer("alice");
        alice.Hand.Add(ForestCard);
        return new LegacyGame([alice, new LegacyPlayer("bob")], deck: []);
    }
}
