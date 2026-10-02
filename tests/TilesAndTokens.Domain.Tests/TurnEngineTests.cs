using NSubstitute;
using static TilesAndTokens.Domain.Tests.TestGame;

namespace TilesAndTokens.Domain.Tests;

public class TurnEngineTests
{
    private readonly IDiceRoller _dice = Substitute.For<IDiceRoller>();
    private readonly TurnEngine _engine;

    public TurnEngineTests()
    {
        _engine = new TurnEngine(_dice);
    }

    [Fact]
    public void RollDice_AddsTheTotal_AndPassesTheTurn()
    {
        _dice.Roll().Returns(new DiceRoll(3, 4));

        var state = Applied(_engine.Apply(TestGame.New(), new RollDice(Alice)));

        state.GetPlayer(Alice).Score.ShouldBe(7);
        state.CurrentPlayer.ShouldBe(Bob);
        state.TurnLog.ShouldHaveSingleItem().Value.ShouldBeOfType<RollDice>();
    }

    [Fact]
    public void RollDice_Doubles_DrawsTheTopCard()
    {
        _dice.Roll().Returns(new DiceRoll(5, 5));

        var state = Applied(_engine.Apply(TestGame.New(), new RollDice(Alice)));

        state.GetPlayer(Alice).Hand.ShouldContain(DeckCard);
        state.Deck.ShouldBeEmpty();
    }

    [Fact]
    public void PlayTileCard_WhenPlaced_PutsTheTileOnTheBoard()
    {
        var state = Applied(_engine.Apply(TestGame.New(), new PlayCard(Alice, Forest.Id, new Position(0, 0))));

        state.Board.TileAt(new Position(0, 0)).ShouldBe(new Tile(new Position(0, 0), Terrain.Forest, Alice));
        state.GetPlayer(Alice).Score.ShouldBe(PlacementValidator.PointsPerTile);
        state.GetPlayer(Alice).TryGetCard(Forest.Id, out _).ShouldBeFalse();
    }

    [Fact]
    public void PlayTileCard_WhenBlocked_IsInvalid()
    {
        var board = Board.Empty.Place(new Tile(new Position(0, 0), Terrain.Plains, Bob));

        var outcome = _engine.Apply(TestGame.New(board), new PlayCard(Alice, Forest.Id, new Position(0, 0)));

        Invalid(outcome).Reason.ShouldContain("occupied");
    }

    [Fact]
    public void PlayTileCard_WhenRejected_IsInvalid_AndNamesTheRule()
    {
        var board = Board.Empty.Place(new Tile(new Position(1, 0), Terrain.Mountain, Alice));

        var outcome = _engine.Apply(TestGame.New(board), new PlayCard(Alice, Water.Id, new Position(0, 0)));

        Invalid(outcome).Reason.ShouldStartWith(nameof(PlacementRuleId.TerrainCompatibility));
    }

    [Fact]
    public void PlayTileCard_WithoutATarget_IsInvalid()
    {
        var outcome = _engine.Apply(TestGame.New(), new PlayCard(Alice, Forest.Id));

        Invalid(outcome).Reason.ShouldContain("target");
    }

    [Fact]
    public void PlayBonusCard_AddsItsPoints()
    {
        var state = Applied(_engine.Apply(TestGame.New(), new PlayCard(Alice, Bonus.Id)));

        state.GetPlayer(Alice).Score.ShouldBe(Bonus.Points);
    }

    [Fact]
    public void PlayCard_NotInHand_IsInvalid()
    {
        var outcome = _engine.Apply(TestGame.New(), new PlayCard(Alice, new CardId("nope")));

        Invalid(outcome).Reason.ShouldContain("doesn't hold");
    }

    [Fact]
    public void Pass_ThreeTimesInARow_EliminatesThePlayer()
    {
        var state = TestGame.New();

        for (var i = 0; i < Player.MaxConsecutivePasses; i++)
        {
            state = Applied(_engine.Apply(state, new Pass(Alice)));
            state = Applied(_engine.Apply(state, new Pass(Bob)));
        }

        state.GetPlayer(Alice).IsEliminated.ShouldBeTrue();
    }

    [Fact]
    public void WrongPlayer_IsInvalid()
    {
        var outcome = _engine.Apply(TestGame.New(), new Pass(Bob));

        Invalid(outcome).Reason.ShouldContain("alice's turn");
    }

    [Fact]
    public void TurnWrapsToTheFirstPlayer_StartsANewRound()
    {
        var state = Applied(_engine.Apply(TestGame.New(), new Pass(Alice)));
        state.Round.ShouldBe(1);

        state = Applied(_engine.Apply(state, new Pass(Bob)));

        state.Round.ShouldBe(2);
        state.CurrentPlayer.ShouldBe(Alice);
    }

    [Fact]
    public void EliminatedPlayers_AreSkipped()
    {
        var state = TestGame.New();
        state = state.WithPlayer(state.GetPlayer(Bob) with { ConsecutivePasses = Player.MaxConsecutivePasses });

        state = Applied(_engine.Apply(state, new Pass(Alice)));

        state.CurrentPlayer.ShouldBe(Alice);
    }

    [Fact]
    public void WinningMove_EndsTheGame()
    {
        // Alice holds tiles from x = 1 to 7 on row 0; placing x = 0 connects the edges.
        var board = new Board([.. Enumerable.Range(1, 7).Select(x => new Tile(new Position(x, 0), Terrain.Plains, Alice))]);

        var outcome = _engine.Apply(TestGame.New(board, new FirstToEdge()), new PlayCard(Alice, Forest.Id, new Position(0, 0)));

        var over = outcome.Value.ShouldBeOfType<GameOver>();
        over.Winner.ShouldBe(Alice);
        over.State.Board.Tiles.Count.ShouldBe(8);
    }

    [Fact]
    public void EliminatingTheSecondToLastPlayer_EndsALastPlayerStandingGame()
    {
        var state = TestGame.New(victory: new LastPlayerStanding());
        state = state.WithPlayer(state.GetPlayer(Alice) with { ConsecutivePasses = Player.MaxConsecutivePasses - 1 });

        var outcome = _engine.Apply(state, new Pass(Alice));

        outcome.Value.ShouldBeOfType<GameOver>().Winner.ShouldBe(Bob);
    }

    private static GameState Applied(TurnOutcome outcome) => outcome switch
    {
        ActionApplied applied => applied.State,
        ActionInvalid invalid => throw new ShouldAssertException($"Expected the action to apply, but it was invalid: {invalid.Reason}"),
        GameOver over => throw new ShouldAssertException($"Expected the game to continue, but {over.Winner} won."),
    };

    private static ActionInvalid Invalid(TurnOutcome outcome) => outcome.Value.ShouldBeOfType<ActionInvalid>();
}
