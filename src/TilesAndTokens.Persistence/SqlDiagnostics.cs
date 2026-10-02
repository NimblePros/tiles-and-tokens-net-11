using System.Text;
using Microsoft.EntityFrameworkCore;

namespace TilesAndTokens.Persistence;

/// <summary>Runs EXPLAIN ANALYZE on the exact SQL (and parameters) EF Core generates for a query.</summary>
public static class SqlDiagnostics
{
    public static async Task<string> ExplainAnalyzeAsync<T>(this IQueryable<T> query, TilesAndTokensDbContext db)
    {
        await using var command = query.CreateDbCommand();
        command.CommandText = "EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF) " + command.CommandText;

        await db.Database.OpenConnectionAsync();
        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            var plan = new StringBuilder();
            while (await reader.ReadAsync())
            {
                plan.AppendLine(reader.GetString(0));
            }

            return plan.ToString();
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    /// <summary>Refreshes planner statistics, so EXPLAIN reflects freshly seeded rows.</summary>
    public static Task AnalyzeAsync(this TilesAndTokensDbContext db) =>
        db.Database.ExecuteSqlRawAsync("""ANALYZE "SavedGames";""");
}
