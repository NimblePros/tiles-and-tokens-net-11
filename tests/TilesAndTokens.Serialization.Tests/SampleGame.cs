using TilesAndTokens.Domain;

namespace TilesAndTokens.Serialization.Tests;

/// <summary>Deterministic games built by the real engine, for round-trip and shape tests.</summary>
internal static class SampleGame
{
    public const int Seed = 7;

    public static readonly PlayerId Alice = new("alice");
    public static readonly PlayerId Bob = new("bob");

    /// <summary>Repeats roll, play, play, pass, so the turn log holds every GameAction case.</summary>
    public static GameState AfterTurns(int turns)
    {
        var engine = new TurnEngine(new SeededDiceRoller(Seed));
        var state = GameSetup.NewGame([Alice, Bob], StandardDeck.Create(), new SeededDeckShuffler(Seed), new MostPoints(Rounds: 20));

        for (var turn = 0; turn < turns; turn++)
        {
            GameAction action = (turn % 4) switch
            {
                0 => new RollDice(state.CurrentPlayer),
                3 => new Pass(state.CurrentPlayer),
                _ => SimpleStrategy.ChooseAction(state),
            };

            state = engine.Apply(state, action) switch
            {
                ActionApplied applied => applied.State,
                ActionInvalid invalid => throw new InvalidOperationException($"Turn {turn} was invalid: {invalid.Reason}"),
                GameOver over => throw new InvalidOperationException($"Turn {turn} ended the game: {over.Winner} won."),
            };
        }

        return state;
    }
}
