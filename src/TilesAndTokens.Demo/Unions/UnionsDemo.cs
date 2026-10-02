using TilesAndTokens.Domain;

namespace TilesAndTokens.Demo.Unions;

/// <summary>Post 2: the turn model rebuilt on unions, played for five scripted turns.</summary>
internal sealed class UnionsDemo : IDemo
{
    private const int Seed = 2026;

    private static readonly PlayerId Alice = new("alice");
    private static readonly PlayerId Bob = new("bob");

    public string Command => "unions";

    public string Title => "Post 2: Discriminated unions";

    public void Run()
    {
        Narrator.Step("The union");
        Narrator.Code("public union GameAction(RollDice, PlayCard, Pass);");
        Narrator.Say("A GameAction holds exactly one of those cases. Every switch over it must handle all three, or the build fails (CS8509).");

        var engine = new TurnEngine(new SeededDiceRoller(Seed));
        var state = GameSetup.NewGame([Alice, Bob], StandardDeck.Create(), new SeededDeckShuffler(Seed), new MostPoints(Rounds: 10));

        Narrator.Step($"Five turns (seed {Seed})");
        PrintHands(state);

        state = Play(engine, state, new RollDice(Alice));
        state = Play(engine, state, new PlayCard(Bob, TileCardIn(state, Bob), new Position(7, 7)));
        state = Play(engine, state, new PlayCard(Alice, TileCardIn(state, Alice), new Position(0, 0)));
        state = Play(engine, state, new PlayCard(Bob, TileCardIn(state, Bob), new Position(0, 0)));
        state = Play(engine, state, new Pass(Bob));
        state = Play(engine, state, new RollDice(Alice));

        Narrator.Step("Final scores");
        foreach (var player in state.Players)
        {
            Narrator.Say($"  {player.Id}: {player.Score} pts, {player.Hand.Count} cards in hand");
        }

        Narrator.Say($"  {state.TurnLog.Count} turns logged. The invalid play didn't use up bob's turn.");
    }

    private static GameState Play(TurnEngine engine, GameState state, GameAction action)
    {
        Narrator.Code(Describe(action));

        switch (engine.Apply(state, action))
        {
            case ActionApplied applied:
                Narrator.Outcome(applied.Summary);
                return applied.State;

            case ActionInvalid invalid:
                Narrator.Problem($"Invalid: {invalid.Reason}");
                return state;

            case GameOver over:
                Narrator.Outcome($"{over.Summary} {over.Winner} wins!");
                return over.State;
        }

        throw new InvalidOperationException("Empty TurnOutcome.");
    }

    // No discard arm here either: the demo breaks too if a new action is added.
    private static string Describe(GameAction action) => action switch
    {
        RollDice roll => $"new RollDice({roll.Player})",
        PlayCard play => $"new PlayCard({play.Player}, {play.Card}, {play.Target})",
        Pass pass => $"new Pass({pass.Player})",
    };

    private static CardId TileCardIn(GameState state, PlayerId player) =>
        state.GetPlayer(player).Hand
            .Select(card => card.Value)
            .OfType<TileCard>()
            .First()
            .Id;

    private static void PrintHands(GameState state)
    {
        foreach (var player in state.Players)
        {
            var hand = player.Hand.Select(card => card switch
            {
                TileCard tile => $"{tile.Terrain} tile",
                BonusPointsCard bonus => $"+{bonus.Points} bonus",
            });
            Narrator.Say($"  {player.Id}'s hand: {string.Join(", ", hand)}");
        }
    }
}
