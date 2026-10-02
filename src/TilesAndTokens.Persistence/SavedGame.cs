using TilesAndTokens.Domain;
using TilesAndTokens.Serialization;

namespace TilesAndTokens.Persistence;

/// <summary>A saved game: the aggregate root for persistence.</summary>
/// <remarks>
/// Snapshot types instead of the domain records: EF Core 11 RC1 can't bind Board's
/// IReadOnlyList&lt;Tile&gt; constructor parameter, can't map the closed VictoryCondition
/// hierarchy as a complex type, and rejects unions ("union types are not supported").
/// </remarks>
public sealed class SavedGame
{
    private SavedGame()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset SavedAt { get; private set; }

    /// <summary>JSON column. Queried by the edge query.</summary>
    public BoardSnapshot Board { get; private set; } = null!;

    /// <summary>Ordinary columns, with indexes on nested properties.</summary>
    public GameSummary Summary { get; private set; } = null!;

    /// <summary>
    /// FALLBACK for unions: the whole GameState (including the GameAction turn log) as written by
    /// the Post 4 serializer. EF Core 11 RC1 can't map union-typed properties.
    /// </summary>
    public string StateJson { get; private set; } = null!;

    public static SavedGame Capture(GameState state, TimeProvider clock)
    {
        var savedAt = clock.GetUtcNow();

        return new SavedGame
        {
            Id = Guid.CreateVersion7(savedAt),
            SavedAt = savedAt,
            Board = BoardSnapshot.From(state.Board),
            Summary = GameSummary.From(state),
            StateJson = GameStateSerializer.ToJson(state),
        };
    }

    public GameState Restore() => GameStateSerializer.FromJson(StateJson);
}
