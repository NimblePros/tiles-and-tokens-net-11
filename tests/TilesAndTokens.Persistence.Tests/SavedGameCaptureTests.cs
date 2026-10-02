using NSubstitute;
using TilesAndTokens.Domain;
using static TilesAndTokens.Persistence.Tests.TestGames;

namespace TilesAndTokens.Persistence.Tests;

/// <summary>Mapping from GameState to the persistence snapshots. No database needed.</summary>
public class SavedGameCaptureTests
{
    [Fact]
    public void Capture_StampsTheTimeFromTheClock()
    {
        var savedAt = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(savedAt);

        var saved = SavedGame.Capture(WithTiles(UniquePlayer("alice"), new FirstToEdge()), clock);

        saved.SavedAt.ShouldBe(savedAt);
    }

    [Fact]
    public void Capture_SummarizesTheLeaderAndTurnCount()
    {
        var alice = UniquePlayer("alice");

        var summary = SavedGame.Capture(WithTiles(alice, new FirstToEdge()), TimeProvider.System).Summary;

        summary.Leader.PlayerId.ShouldBe(alice.Value);
        summary.Leader.Score.ShouldBe(10);
        summary.TurnCount.ShouldBe(3);
        summary.Round.ShouldBe(2);
    }

    public static TheoryData<VictoryCondition, string, int?> Victories => new()
    {
        { new MostPoints(Rounds: 12), VictorySnapshot.MostPoints, 12 },
        { new FirstToEdge(), VictorySnapshot.FirstToEdge, null },
        { new LastPlayerStanding(), VictorySnapshot.LastPlayerStanding, null },
    };

    [Theory]
    [MemberData(nameof(Victories))]
    public void Capture_FlattensEveryVictoryCondition(VictoryCondition victory, string kind, int? rounds)
    {
        var snapshot = VictorySnapshot.From(victory);

        snapshot.Kind.ShouldBe(kind);
        snapshot.Rounds.ShouldBe(rounds);
    }
}
