using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TilesAndTokens.Persistence.Migrations;

/// <summary>
/// POSTGRES-NATIVE, hand-written: a GIN index over the Board jsonb column for containment (@>)
/// queries. Not expressible in the EF Core model: Npgsql 11 RC1 turns an EF index into Board.Tiles
/// into a B-tree over the whole column. Used by SavedGameQueries.WithTileOnEdgeUsingGin.
/// </summary>
public partial class _20261002161723_AddBoardGinIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""CREATE INDEX "IX_SavedGames_Board_Gin" ON "SavedGames" USING GIN ("Board" jsonb_path_ops);""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP INDEX "IX_SavedGames_Board_Gin";""");
    }
}
