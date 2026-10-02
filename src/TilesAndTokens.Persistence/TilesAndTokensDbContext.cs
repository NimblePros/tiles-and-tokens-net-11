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

        // Not configured: an index into Board.Tiles. EF Core 11 accepts one
        // (e.g. HasIndex(g => g.Board.Tiles[0].OwnerId)), but Npgsql 11 RC1 generates a B-tree
        // over the whole "Board" column, which the edge query can't use. See docs/feature-status.md.
    });
}
