using TilesAndTokens.Legacy.StringFlags;

namespace TilesAndTokens.Legacy.Tests;

public class StringTurnTests
{
    [Fact]
    public void RollDice_AddsTheTotalToTheScore()
    {
        var game = GameFixture.NewGame();

        StringTurnProcessor.Apply(game, new StringTurn { PlayerId = "alice", ActionType = "RollDice", DiceTotal = 7 });

        game.Player("alice").Score.ShouldBe(7);
    }

    [Fact]
    public void TypoInActionType_CompilesAndSilentlyDoesNothing()
    {
        var game = GameFixture.NewGame();

        // "Rolldice" instead of "RollDice". The compiler has no idea.
        var message = StringTurnProcessor.Apply(game, new StringTurn { PlayerId = "alice", ActionType = "Rolldice", DiceTotal = 7 });

        message.ShouldBeEmpty();
        game.Player("alice").Score.ShouldBe(0);
        game.Player("alice").ConsecutivePasses.ShouldBe(0);
    }

    [Fact]
    public void RollDiceWithoutATotal_CompilesButThrowsAtRuntime()
    {
        var game = GameFixture.NewGame();

        Should.Throw<InvalidOperationException>(() =>
            StringTurnProcessor.Apply(game, new StringTurn { PlayerId = "alice", ActionType = "RollDice" }));
    }
}
