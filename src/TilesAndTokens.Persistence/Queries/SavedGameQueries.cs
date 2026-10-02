using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TilesAndTokens.Domain;

namespace TilesAndTokens.Persistence;

/// <summary>The Post 5 queries. They return IQueryable so callers can see the SQL with ToQueryString().</summary>
public sealed class SavedGameQueries(TilesAndTokensDbContext db)
{
    private const int LastIndex = Board.Size - 1;

    /// <summary>Games where the player owns a tile on any edge. Runs over the Board JSON column.</summary>
    public IQueryable<SavedGame> WithTileOnEdge(PlayerId player)
    {
        var playerId = player.Value;

        return db.SavedGames.AsNoTracking().Where(g => g.Board.Tiles.Any(t =>
            t.OwnerId == playerId
            && (t.Position.X == 0 || t.Position.X == LastIndex || t.Position.Y == 0 || t.Position.Y == LastIndex)));
    }

    /// <summary>
    /// POSTGRES-NATIVE, not an EF Core feature: the same edge query written as jsonb containment
    /// (<c>@&gt;</c>), so it can use the GIN index added by the AddBoardGinIndex migration.
    /// </summary>
    /// <remarks>
    /// Raw SQL selects only ids (FromSql on SavedGame throws NullReferenceException in EF Core 11 RC1,
    /// and EF.Functions.JsonContains doesn't translate for a complex JSON property). The ids are
    /// composed into an ordinary LINQ query, which EF wraps in WHERE "Id" IN (...).
    /// </remarks>
    public IQueryable<SavedGame> WithTileOnEdgeUsingGin(PlayerId player)
    {
        var left = EdgeTile(player, "X", 0);
        var right = EdgeTile(player, "X", LastIndex);
        var top = EdgeTile(player, "Y", 0);
        var bottom = EdgeTile(player, "Y", LastIndex);

        var ids = db.Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value" FROM "SavedGames"
            WHERE "Board" @> {left}::jsonb OR "Board" @> {right}::jsonb OR "Board" @> {top}::jsonb OR "Board" @> {bottom}::jsonb
            """);

        return db.SavedGames.AsNoTracking().Where(g => ids.Contains(g.Id));
    }

    /// <summary>Games using a victory condition kind, highest leader score first. Uses the nested-property index.</summary>
    public IQueryable<SavedGame> UsingVictory(string victoryKind) =>
        db.SavedGames.AsNoTracking()
            .Where(g => g.Summary.Victory.Kind == victoryKind)
            .OrderByDescending(g => g.Summary.Leader.Score);

    /// <summary>Games where the player is in the lead. Uses the nested-property index.</summary>
    public IQueryable<SavedGame> LedBy(PlayerId player)
    {
        var playerId = player.Value;

        return db.SavedGames.AsNoTracking().Where(g => g.Summary.Leader.PlayerId == playerId);
    }

    /// <summary>A JSON fragment matching one of the player's tiles on one edge, e.g. Position.X = 0.</summary>
    private static string EdgeTile(PlayerId player, string axis, int value) =>
        JsonSerializer.Serialize(new { Tiles = new[] { new { OwnerId = player.Value, Position = new Dictionary<string, int> { [axis] = value } } } });
}
