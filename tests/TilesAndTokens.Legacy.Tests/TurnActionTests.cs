using TilesAndTokens.Legacy.NullableWrapper;

namespace TilesAndTokens.Legacy.Tests;

public class TurnActionTests
{
    [Fact]
    public void PlayCard_PlacesTheTile()
    {
        var game = GameFixture.NewGame();

        TurnActionProcessor.Apply(game, new TurnAction { PlayerId = "alice", Card = GameFixture.ForestCard, Target = new Position(0, 0) });

        game.Board.TileAt(new Position(0, 0)).ShouldNotBeNull();
    }

    [Fact]
    public void RollAndCardBothSet_IsConstructible_AndTheCardIsSilentlyIgnored()
    {
        var game = GameFixture.NewGame();

        // Rolling dice AND playing a card in one turn is against the rules,
        // but this object is perfectly happy to represent it.
        var invalid = new TurnAction
        {
            PlayerId = "alice",
            Roll = new DiceRoll(3, 4),
            Card = GameFixture.ForestCard,
            Target = new Position(0, 0),
        };

        TurnActionProcessor.Apply(game, invalid);

        game.Player("alice").Score.ShouldBe(7);
        game.Board.Tiles.ShouldBeEmpty();
        game.Player("alice").Hand.ShouldContain(GameFixture.ForestCard);
    }

    [Fact]
    public void NothingSet_CompilesButThrowsAtRuntime()
    {
        var game = GameFixture.NewGame();

        Should.Throw<InvalidOperationException>(() =>
            TurnActionProcessor.Apply(game, new TurnAction { PlayerId = "alice" }));
    }
}
