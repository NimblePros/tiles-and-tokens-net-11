using static TilesAndTokens.Domain.Tests.TestGame;

namespace TilesAndTokens.Domain.Tests;

public class VictoryCheckerTests
{
    [Fact]
    public void MostPoints_NoWinnerBeforeTheLastRoundEnds()
    {
        var state = WithScores(alice: 10, bob: 5) with { Round = 3 };

        VictoryChecker.Check(state, new MostPoints(Rounds: 3)).ShouldBeNull();
    }

    [Fact]
    public void MostPoints_HighestScoreWinsAfterTheLastRound()
    {
        var state = WithScores(alice: 5, bob: 10) with { Round = 4 };

        VictoryChecker.Check(state, new MostPoints(Rounds: 3)).ShouldBe(Bob);
    }

    [Fact]
    public void MostPoints_TieGoesToTheEarlierSeat()
    {
        var state = WithScores(alice: 7, bob: 7) with { Round = 4 };

        VictoryChecker.Check(state, new MostPoints(Rounds: 3)).ShouldBe(Alice);
    }

    [Fact]
    public void FirstToEdge_NoWinnerWithoutAConnectedPath()
    {
        // A row with a gap at x = 4.
        var board = Row(Alice, y: 2, xs: [0, 1, 2, 3, 5, 6, 7]);

        VictoryChecker.Check(TestGame.New(board), new FirstToEdge()).ShouldBeNull();
    }

    [Fact]
    public void FirstToEdge_LeftToRight()
    {
        var board = Row(Alice, y: 2, xs: [0, 1, 2, 3, 4, 5, 6, 7]);

        VictoryChecker.Check(TestGame.New(board), new FirstToEdge()).ShouldBe(Alice);
    }

    [Fact]
    public void FirstToEdge_TopToBottom_AlongAWindingPath()
    {
        // Down column 0 to y = 3, across to x = 1, then down to the bottom edge.
        Position[] path = [new(0, 0), new(0, 1), new(0, 2), new(0, 3), new(1, 3), new(1, 4), new(1, 5), new(1, 6), new(1, 7)];
        var board = new Board([.. path.Select(p => new Tile(p, Terrain.Forest, Bob))]);

        VictoryChecker.Check(TestGame.New(board), new FirstToEdge()).ShouldBe(Bob);
    }

    [Fact]
    public void FirstToEdge_OtherPlayersTilesDontCount()
    {
        var board = Row(Alice, y: 2, xs: [0, 1, 2, 3, 5, 6, 7]).Place(new Tile(new Position(4, 2), Terrain.Plains, Bob));

        VictoryChecker.Check(TestGame.New(board), new FirstToEdge()).ShouldBeNull();
    }

    [Fact]
    public void LastPlayerStanding_NoWinnerWhileTwoRemain() =>
        VictoryChecker.Check(TestGame.New(), new LastPlayerStanding()).ShouldBeNull();

    [Fact]
    public void LastPlayerStanding_TheOnlyActivePlayerWins()
    {
        var state = TestGame.New();
        state = state.WithPlayer(state.GetPlayer(Alice) with { ConsecutivePasses = Player.MaxConsecutivePasses });

        VictoryChecker.Check(state, new LastPlayerStanding()).ShouldBe(Bob);
    }

    private static GameState WithScores(int alice, int bob)
    {
        var state = TestGame.New();
        return state
            .WithPlayer(state.GetPlayer(Alice) with { Score = alice })
            .WithPlayer(state.GetPlayer(Bob) with { Score = bob });
    }

    private static Board Row(PlayerId owner, int y, int[] xs) =>
        new([.. xs.Select(x => new Tile(new Position(x, y), Terrain.Plains, owner))]);
}
