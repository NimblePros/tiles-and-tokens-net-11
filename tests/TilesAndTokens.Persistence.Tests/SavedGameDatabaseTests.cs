using Microsoft.EntityFrameworkCore;
using TilesAndTokens.Domain;
using TilesAndTokens.Serialization;
using static TilesAndTokens.Persistence.Tests.TestGames;

namespace TilesAndTokens.Persistence.Tests;

[Collection(PostgresCollection.Name)]
public class SavedGameDatabaseTests(PostgresFixture postgres)
{
    [Fact]
    public async Task SaveAndLoad_RestoresTheWholeGameState()
    {
        var state = GameGenerator.Generate(count: 1, seed: 11).Single();
        var saved = await SaveAsync(SavedGame.Capture(state, TimeProvider.System));

        await using var db = postgres.CreateDbContext();
        var loaded = await db.SavedGames.SingleAsync(g => g.Id == saved.Id, TestContext.Current.CancellationToken);

        // jsonb reorders object keys, so compare the restored state re-serialized, not the stored text.
        GameStateSerializer.ToJson(loaded.Restore()).ShouldBe(GameStateSerializer.ToJson(state));
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsTheNestedComplexTypes()
    {
        var alice = UniquePlayer("alice");
        var state = WithTiles(alice, new MostPoints(Rounds: 7), TileAt(0, 3, alice), new Tile(new Position(1, 3), Terrain.Water, alice));
        var saved = await SaveAsync(SavedGame.Capture(state, TimeProvider.System));

        await using var db = postgres.CreateDbContext();
        var loaded = await db.SavedGames.SingleAsync(g => g.Id == saved.Id, TestContext.Current.CancellationToken);

        loaded.Board.Tiles.Select(t => (t.Position.X, t.Position.Y, t.Terrain, t.OwnerId))
            .ShouldBe([(0, 3, Terrain.Plains, alice.Value), (1, 3, Terrain.Water, alice.Value)]);
        loaded.Summary.Leader.PlayerId.ShouldBe(alice.Value);
        loaded.Summary.Victory.Kind.ShouldBe(VictorySnapshot.MostPoints);
        loaded.Summary.Victory.Rounds.ShouldBe(7);
    }

    [Fact]
    public async Task EdgeQuery_FindsOnlyGamesWhereThePlayerOwnsAnEdgeTile()
    {
        var alice = UniquePlayer("alice");
        var bob = UniquePlayer("bob");

        var leftEdge = await SaveAsync(SavedGame.Capture(WithTiles(alice, new FirstToEdge(), TileAt(0, 4, alice)), TimeProvider.System));
        var bottomEdge = await SaveAsync(SavedGame.Capture(WithTiles(alice, new FirstToEdge(), TileAt(3, 7, alice)), TimeProvider.System));
        await SaveAsync(SavedGame.Capture(WithTiles(alice, new FirstToEdge(), TileAt(3, 3, alice)), TimeProvider.System));
        await SaveAsync(SavedGame.Capture(WithTiles(alice, new FirstToEdge(), TileAt(0, 0, bob), TileAt(4, 4, alice)), TimeProvider.System));

        await using var db = postgres.CreateDbContext();
        var ids = await new SavedGameQueries(db).WithTileOnEdge(alice).Select(g => g.Id).ToListAsync(TestContext.Current.CancellationToken);

        ids.ShouldBe([leftEdge.Id, bottomEdge.Id], ignoreOrder: true);
    }

    [Fact]
    public async Task GinEdgeQuery_FindsTheSameGamesAsTheLinqQuery()
    {
        var alice = UniquePlayer("alice");
        var bob = UniquePlayer("bob");
        await SaveAsync(SavedGame.Capture(WithTiles(alice, new FirstToEdge(), TileAt(7, 2, alice)), TimeProvider.System));
        await SaveAsync(SavedGame.Capture(WithTiles(alice, new FirstToEdge(), TileAt(5, 0, alice)), TimeProvider.System));
        await SaveAsync(SavedGame.Capture(WithTiles(alice, new FirstToEdge(), TileAt(3, 3, alice), TileAt(7, 7, bob)), TimeProvider.System));

        await using var db = postgres.CreateDbContext();
        var queries = new SavedGameQueries(db);
        var viaLinq = await queries.WithTileOnEdge(alice).Select(g => g.Id).ToListAsync(TestContext.Current.CancellationToken);
        var viaGin = await queries.WithTileOnEdgeUsingGin(alice).Select(g => g.Id).ToListAsync(TestContext.Current.CancellationToken);

        viaLinq.Count.ShouldBe(2);
        viaGin.ShouldBe(viaLinq, ignoreOrder: true);
    }

    [Fact]
    public async Task VictoryQuery_FiltersByKind_HighestLeaderScoreFirst()
    {
        var leader = UniquePlayer("leader");
        var low = WithTiles(leader, new LastPlayerStanding());
        var high = low.WithPlayer(low.GetPlayer(leader) with { Score = 99 });
        var savedLow = await SaveAsync(SavedGame.Capture(low, TimeProvider.System));
        var savedHigh = await SaveAsync(SavedGame.Capture(high, TimeProvider.System));

        await using var db = postgres.CreateDbContext();
        var ids = await new SavedGameQueries(db).UsingVictory(VictorySnapshot.LastPlayerStanding)
            .Where(g => g.Summary.Leader.PlayerId == leader.Value)
            .Select(g => g.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        ids.ShouldBe([savedHigh.Id, savedLow.Id]);
    }

    [Theory]
    [InlineData("IX_SavedGames_Summary_Leader_PlayerId", "(\"Summary_Leader_PlayerId\")")]
    [InlineData("IX_SavedGames_Summary_Victory_Kind_Summary_Leader_Score", "(\"Summary_Victory_Kind\", \"Summary_Leader_Score\")")]
    public async Task NestedComplexPropertyIndexes_ExistInPostgres(string indexName, string columns) =>
        (await IndexDefinitionAsync(indexName)).ShouldEndWith($"USING btree {columns}");

    [Fact]
    public async Task BoardGinIndex_ExistsInPostgres() =>
        (await IndexDefinitionAsync("IX_SavedGames_Board_Gin")).ShouldEndWith("USING gin (\"Board\" jsonb_path_ops)");

    private async Task<string> IndexDefinitionAsync(string indexName)
    {
        await using var db = postgres.CreateDbContext();

        var definitions = await db.Database
            .SqlQuery<string>($"SELECT indexdef AS \"Value\" FROM pg_indexes WHERE tablename = 'SavedGames' AND indexname = {indexName}")
            .ToListAsync(TestContext.Current.CancellationToken);

        return definitions.ShouldHaveSingleItem();
    }

    private async Task<SavedGame> SaveAsync(SavedGame game)
    {
        await using var db = postgres.CreateDbContext();
        db.SavedGames.Add(game);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return game;
    }
}
