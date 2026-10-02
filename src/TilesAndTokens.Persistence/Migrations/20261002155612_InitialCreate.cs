using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TilesAndTokens.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261002155612_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SavedGames",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SavedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                StateJson = table.Column<string>(type: "jsonb", nullable: false),
                Summary_Round = table.Column<int>(type: "integer", nullable: false),
                Summary_TurnCount = table.Column<int>(type: "integer", nullable: false),
                Summary_Leader_PlayerId = table.Column<string>(type: "text", nullable: false),
                Summary_Leader_Score = table.Column<int>(type: "integer", nullable: false),
                Summary_Victory_Kind = table.Column<string>(type: "text", nullable: false),
                Summary_Victory_Rounds = table.Column<int>(type: "integer", nullable: true),
                Board = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavedGames", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SavedGames_Summary_Leader_PlayerId",
            table: "SavedGames",
            column: "Summary_Leader_PlayerId");

        migrationBuilder.CreateIndex(
            name: "IX_SavedGames_Summary_Victory_Kind_Summary_Leader_Score",
            table: "SavedGames",
            columns: new[] { "Summary_Victory_Kind", "Summary_Leader_Score" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SavedGames");
    }
}
