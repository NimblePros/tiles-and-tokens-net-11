using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TilesAndTokens.Demo.EfCore;
using TilesAndTokens.Domain;
using TilesAndTokens.Persistence;

namespace TilesAndTokens.Demo.Bench;

/// <summary>
/// Not a milestone, so `all` doesn't run it: the 20,000-game benchmark behind
/// docs/post-5-sql-and-plans.md. Empties SavedGames, seeds deterministic games, and prints the
/// plans and median timings for the edge query in LINQ vs. jsonb containment (GIN).
/// </summary>
/// <remarks>
/// Under the AppHost it's the "bench" resource, with its own database: start it from the dashboard.
/// </remarks>
internal sealed class BenchCommand : IDemo
{
    private const int Games = 20_000;
    private const int Seed = 2026;
    private const int BatchSize = 1_000;
    private const int Runs = 9;

    public string Command => "bench";

    public string Title => $"Post 5 benchmark: {Games:N0} games, LINQ over JSON vs. GIN containment";

    public void Run() => RunAsync().GetAwaiter().GetResult();

    private static async Task RunAsync()
    {
        var connectionString = Host.CreateApplicationBuilder().Configuration.GetConnectionString(EfCoreDemo.ConnectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Narrator.Problem("Skipped: no database. Run the AppHost and start the \"bench\" resource from the dashboard:");
            Narrator.Code("dotnet run --project src/TilesAndTokens.AppHost");
            return;
        }

        var options = new DbContextOptionsBuilder<TilesAndTokensDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new TilesAndTokensDbContext(options);
        await db.Database.MigrateAsync();
        Narrator.Outcome(await db.Database.SqlQueryRaw<string>("SELECT version() AS \"Value\"").SingleAsync());

        await SeedAsync(db, options);

        var queries = new SavedGameQueries(db);
        var player = new PlayerId("player-042");
        var linq = queries.WithTileOnEdge(player);
        var gin = queries.WithTileOnEdgeUsingGin(player);

        Narrator.Step("Matches");
        var linqIds = await linq.Select(g => g.Id).OrderBy(id => id).ToListAsync();
        var ginIds = await gin.Select(g => g.Id).OrderBy(id => id).ToListAsync();
        Narrator.Outcome($"rows: {await db.SavedGames.CountAsync()}, LINQ matches: {linqIds.Count}, GIN matches: {ginIds.Count}, same ids: {linqIds.SequenceEqual(ginIds)}");

        await ShowPlanAsync("LINQ edge query", linq, db);
        await ShowPlanAsync("Containment, planner's choice", gin, db);

        // SET only lasts for the session, so keep one connection open for the forced-GIN measurements.
        await db.Database.OpenConnectionAsync();
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET enable_seqscan = off;");
            await ShowPlanAsync("Containment, enable_seqscan = off (comparison only)", gin, db);
            var forced = await MedianMsAsync(() => gin.Select(g => g.Id).ToListAsync());
            await db.Database.ExecuteSqlRawAsync("RESET enable_seqscan;");

            var linqMs = await MedianMsAsync(() => linq.Select(g => g.Id).ToListAsync());
            var ginMs = await MedianMsAsync(() => gin.Select(g => g.Id).ToListAsync());

            Narrator.Step($"Timings (median of {Runs} runs after 1 warm-up, ids only)");
            Narrator.Outcome($"LINQ over JSON:                     {linqMs,7:N1} ms");
            Narrator.Outcome($"Containment, planner's choice:      {ginMs,7:N1} ms");
            Narrator.Outcome($"Containment, enable_seqscan = off:  {forced,7:N1} ms");
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task SeedAsync(TilesAndTokensDbContext db, DbContextOptions<TilesAndTokensDbContext> options)
    {
        Narrator.Step($"Seed {Games:N0} games (seed {Seed}), after emptying SavedGames");
        await db.Database.ExecuteSqlRawAsync("""TRUNCATE "SavedGames";""");

        var stopwatch = Stopwatch.StartNew();
        foreach (var batch in GameGenerator.Generate(Games, Seed).Chunk(BatchSize))
        {
            await using var batchDb = new TilesAndTokensDbContext(options);
            batchDb.SavedGames.AddRange(batch.Select(state => SavedGame.Capture(state, TimeProvider.System)));
            await batchDb.SaveChangesAsync();
        }

        await db.AnalyzeAsync();
        Narrator.Outcome($"Saved {Games:N0} games in {stopwatch.Elapsed.TotalSeconds:N0} s, then ran ANALYZE.");
    }

    private static async Task ShowPlanAsync(string title, IQueryable<SavedGame> query, TilesAndTokensDbContext db)
    {
        Narrator.Step($"Plan: {title}");
        Narrator.Code((await query.ExplainAnalyzeAsync(db)).TrimEnd().ReplaceLineEndings("\n    "));
    }

    private static async Task<double> MedianMsAsync(Func<Task> run)
    {
        await run();

        var times = new List<double>(Runs);
        for (var i = 0; i < Runs; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await run();
            times.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        times.Sort();
        return times[Runs / 2];
    }
}
