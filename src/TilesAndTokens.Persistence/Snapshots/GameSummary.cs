using TilesAndTokens.Domain;

namespace TilesAndTokens.Persistence;

/// <summary>
/// Mapped to ordinary columns (table splitting), so its nested properties can be indexed:
/// Summary.Leader.PlayerId becomes the column Summary_Leader_PlayerId.
/// </summary>
public sealed class GameSummary
{
    public int Round { get; set; }

    public int TurnCount { get; set; }

    public LeaderSnapshot Leader { get; set; } = new();

    public VictorySnapshot Victory { get; set; } = new();

    /// <remarks>The leader is the highest score; ties go to the earlier seat.</remarks>
    public static GameSummary From(GameState state)
    {
        var leader = state.Players.MaxBy(p => p.Score)!;

        return new GameSummary
        {
            Round = state.Round,
            TurnCount = state.TurnLog.Count,
            Leader = new LeaderSnapshot { PlayerId = leader.Id.Value, Score = leader.Score },
            Victory = VictorySnapshot.From(state.Victory),
        };
    }
}
