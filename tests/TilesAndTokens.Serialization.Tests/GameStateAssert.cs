using TilesAndTokens.Domain;

namespace TilesAndTokens.Serialization.Tests;

/// <summary>
/// Structural equality for GameState. The records hold IReadOnlyList properties, and record
/// equality compares those by reference, so a loaded state never equals the original with ==.
/// </summary>
internal static class GameStateAssert
{
    public static void ShouldBeEquivalentTo(this GameState actual, GameState expected)
    {
        actual.Board.Tiles.ShouldBe(expected.Board.Tiles);
        actual.Deck.ShouldBe(expected.Deck);
        actual.Victory.ShouldBe(expected.Victory);
        actual.TurnLog.ShouldBe(expected.TurnLog);
        actual.Round.ShouldBe(expected.Round);
        actual.CurrentPlayer.ShouldBe(expected.CurrentPlayer);

        actual.Players.Count.ShouldBe(expected.Players.Count);
        for (var i = 0; i < expected.Players.Count; i++)
        {
            actual.Players[i].ShouldBe(expected.Players[i] with { Hand = actual.Players[i].Hand });
            actual.Players[i].Hand.ShouldBe(expected.Players[i].Hand);
        }
    }
}
