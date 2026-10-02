using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;

namespace TilesAndTokens.Persistence.Tests;

/// <summary>
/// Starts the AppHost's Postgres (and only that) once per test collection, then applies migrations.
/// Needs a running container runtime (Docker).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public string ConnectionString { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TilesAndTokens_AppHost>();

        // The tests only need the database: drop the demo runner and the pgweb UI.
        foreach (var resource in builder.Resources.Where(r => r.Name == "demo" || r is PgWebContainerResource).ToList())
        {
            builder.Resources.Remove(resource);
        }

        _app = await builder.BuildAsync();
        await _app.StartAsync();
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("tilesandtokens");

        ConnectionString = await _app.GetConnectionStringAsync("tilesandtokens")
            ?? throw new InvalidOperationException("No connection string for 'tilesandtokens'.");

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public TilesAndTokensDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<TilesAndTokensDbContext>().UseNpgsql(ConnectionString).Options);

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
