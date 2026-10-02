using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TilesAndTokens.Domain;
using TilesAndTokens.Persistence;

namespace TilesAndTokens.Demo.EfCore;

/// <summary>Post 5 and the series finale: EF Core 11 with nested complex types, run under Aspire.</summary>
internal sealed class EfCoreDemo : IDemo
{
    public const string ConnectionName = "tilesandtokens";

    private const int SeedGames = 1_000;
    private const int Seed = 2026;

    public string Command => "efcore";

    public string Title => "Post 5: EF Core 11 and nested complex types";

    public void Run() => RunAsync().GetAwaiter().GetResult();

    private static async Task RunAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(ConnectionName)))
        {
            Narrator.Problem("Skipped: no database. Run the whole series with the Aspire AppHost:");
            Narrator.Code("dotnet run --project src/TilesAndTokens.AppHost");
            return;
        }

        // Keep the narrated console output readable: logs (including every SQL command) still
        // go to the Aspire dashboard through OpenTelemetry, just not to the console.
        builder.Logging.ClearProviders();
        builder.AddServiceDefaults();
        builder.AddNpgsqlDbContext<TilesAndTokensDbContext>(ConnectionName);
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TilesAndTokensDbContext>();
        var queries = new SavedGameQueries(db);

        Narrator.Step("Migrate");
        await db.Database.MigrateAsync();
        Narrator.Outcome("Applied migrations at startup (dev-only shortcut).");

        await SeedAsync(db);

        Narrator.Step("Query 1: games where player-042 owns a tile on an edge (Board JSON column)");
        await ShowQueryAsync(queries.WithTileOnEdge(new PlayerId("player-042")), db);

        Narrator.Step("Query 1b: the same, as jsonb containment (Postgres-native GIN index, not an EF feature)");
        await ShowQueryAsync(queries.WithTileOnEdgeUsingGin(new PlayerId("player-042")), db);
        Narrator.Say($"  At {SeedGames:N0} games the table is small enough that Postgres may still scan it.");
        Narrator.Say("  At 20,000 games it uses the GIN index (see docs/post-5-sql-and-plans.md).");

        Narrator.Step("Query 2: firstToEdge games by leader score (index on nested complex properties)");
        await ShowQueryAsync(queries.UsingVictory(VictorySnapshot.FirstToEdge).Take(5), db);

        Narrator.Step("Query 3: games led by player-042 (index on Summary.Leader.PlayerId)");
        await ShowQueryAsync(queries.LedBy(new PlayerId("player-042")), db);

        await FinaleAsync(db, queries);
    }

    private static async Task SeedAsync(TilesAndTokensDbContext db)
    {
        Narrator.Step($"Seed {SeedGames:N0} games");
        var existing = await db.SavedGames.CountAsync();
        if (existing >= SeedGames)
        {
            Narrator.Outcome($"{existing:N0} games already saved.");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        db.SavedGames.AddRange(GameGenerator.Generate(SeedGames - existing, Seed)
            .Select(state => SavedGame.Capture(state, TimeProvider.System)));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await db.AnalyzeAsync();
        Narrator.Outcome($"Saved {SeedGames - existing:N0} games in {stopwatch.ElapsedMilliseconds:N0} ms, then ran ANALYZE.");
    }

    private static async Task ShowQueryAsync(IQueryable<SavedGame> query, TilesAndTokensDbContext db)
    {
        Narrator.Code(query.ToQueryString().ReplaceLineEndings("\n    "));

        var stopwatch = Stopwatch.StartNew();
        var count = (await query.ToListAsync()).Count;
        Narrator.Outcome($"{count} games in {stopwatch.ElapsedMilliseconds} ms");

        Narrator.Say("  Plan:");
        Narrator.Code((await query.ExplainAnalyzeAsync(db)).TrimEnd().ReplaceLineEndings("\n    "));
    }

    /// <summary>The whole series in one pass: union action, closed rules, STJ snapshot, EF save, query.</summary>
    private static async Task FinaleAsync(TilesAndTokensDbContext db, SavedGameQueries queries)
    {
        Narrator.Step("Finale: the whole pipeline");
        var alice = new PlayerId("alice-finale");
        var bob = new PlayerId("bob-finale");
        var engine = new TurnEngine(new SeededDiceRoller(Seed));
        var state = GameSetup.NewGame([alice, bob], StandardDeck.Create(), new SeededDeckShuffler(Seed), new FirstToEdge());

        // Post 2 + 3: union actions, validated by the closed placement rules.
        for (var turn = 0; turn < 6; turn++)
        {
            var action = SimpleStrategy.ChooseAction(state);
            if (engine.Apply(state, action) is ActionApplied applied)
            {
                Narrator.Outcome(applied.Summary);
                state = applied.State;
            }
        }

        // Post 4 + 5: STJ snapshot inside an EF Core entity.
        var saved = SavedGame.Capture(state, TimeProvider.System);
        db.SavedGames.Add(saved);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Narrator.Outcome($"Saved game {saved.Id}: {saved.Board.Tiles.Count} tiles, leader {saved.Summary.Leader.PlayerId}.");

        var found = await queries.WithTileOnEdge(alice).AnyAsync(g => g.Id == saved.Id);
        Narrator.Outcome($"Edge query finds it for {alice}: {found}");

        var loaded = (await db.SavedGames.AsNoTracking().SingleAsync(g => g.Id == saved.Id)).Restore();
        Narrator.Outcome($"Restored from the database: round {loaded.Round}, {loaded.TurnLog.Count} logged actions " +
            $"({string.Join(", ", loaded.TurnLog.Select(a => a.Value?.GetType().Name))}), victory {loaded.Victory.Name}.");
    }
}
