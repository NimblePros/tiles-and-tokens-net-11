using static TilesAndTokens.Domain.Tests.TestGame;

namespace TilesAndTokens.Domain.Tests;

public class GameActionTests
{
    public static TheoryData<GameAction, string> EveryCase => new()
    {
        { new RollDice(Alice), "roll" },
        { new PlayCard(Alice, Forest.Id, new Position(0, 0)), "play" },
        { new Pass(Alice), "pass" },
    };

    [Theory]
    [MemberData(nameof(EveryCase))]
    public void EveryCase_ConvertsImplicitly_AndIsHandled(GameAction action, string expected)
    {
        var handled = action switch
        {
            RollDice => "roll",
            PlayCard => "play",
            Pass => "pass",
        };

        handled.ShouldBe(expected);
    }

    [Fact]
    public void Player_ComesFromWhicheverCaseIsHeld()
    {
        GameAction action = new Pass(Bob);

        action.Player.ShouldBe(Bob);
    }

    [Fact]
    public void Value_IsTheCaseInstance()
    {
        var roll = new RollDice(Alice);
        GameAction action = roll;

        action.Value.ShouldBeSameAs(roll);
    }

    [Fact]
    public void Default_IsAnEmptyUnion_ThatNoCaseMatches()
    {
        // Unions are structs, so default(GameAction) exists. It holds no case, and the
        // compiler doesn't flag a switch that only handles the declared cases.
        GameAction empty = default;

        empty.Value.ShouldBeNull();
        Should.Throw<System.Runtime.CompilerServices.SwitchExpressionException>(() => empty.Player);
    }
}
