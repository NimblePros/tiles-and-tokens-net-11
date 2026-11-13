using Microsoft.EntityFrameworkCore;

namespace TilesAndTokens.Persistence;

public sealed class TilesAndTokensDbContext(DbContextOptions<TilesAndTokensDbContext> options) : DbContext(options)
{
    public DbSet<SavedGame> SavedGames => Set<SavedGame>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<SavedGame>(game =>
    {
        game.HasKey(g => g.Id);

        // Board -> Tiles -> Position: nested complex types in one jsonb column.
        game.ComplexProperty(g => g.Board, board =>
        {
            board.ToJson();
            board.ComplexCollection(b => b.Tiles, tile =>
            {
                tile.ComplexProperty(t => t.Position);
                tile.Property(t => t.Terrain).HasConversion<string>();
            });
        });

        // Summary -> Leader / Victory: nested complex types in ordinary columns (table splitting).
        game.ComplexProperty(g => g.Summary, summary =>
        {
            summary.ComplexProperty(s => s.Leader);
            summary.ComplexProperty(s => s.Victory);
        });

        game.Property(g => g.StateJson).HasColumnType("jsonb");

        // EF Core 11: indexes on properties of nested complex types.
        game.HasIndex(g => g.Summary.Leader.PlayerId);
        game.HasIndex(g => new { g.Summary.Victory.Kind, g.Summary.Leader.Score });

        // Not configured: an index into Board.Tiles. Npgsql 11 rc.3 turns HasIndex(g => g.Board.Tiles[0].OwnerId)
        // into a path index on one fixed tile, and rejects an index over all tiles. The edge query
        // needs "any tile", so neither helps. See docs/feature-status.md.
    });
}
