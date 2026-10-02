using TilesAndTokens.Legacy.BaseClass;

namespace TilesAndTokens.Legacy.Tests;

public class LegacyActionTests
{
    [Fact]
    public void Pass_IncrementsConsecutivePasses()
    {
        var game = GameFixture.NewGame();

        LegacyActionProcessor.Apply(game, new PassAction("bob"));

        game.Player("bob").ConsecutivePasses.ShouldBe(1);
    }

    [Fact]
    public void NewSubclass_CompilesButTheCastChainThrowsAtRuntime()
    {
        var game = GameFixture.NewGame();
        LegacyAction trade = new TradeAction("alice", "bob", giveCardId: "forest-1", takeCardId: "water-1");

        var ex = Should.Throw<NotSupportedException>(() => LegacyActionProcessor.Apply(game, trade));

        ex.Message.ShouldContain(nameof(TradeAction));
    }
}
