var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgWeb();

var database = postgres.AddDatabase("tilesandtokens");

// Runs the whole series, in order, as the talk's closing demo. Under the AppHost there's no
// interactive console, so the demo runner doesn't pause between sections.
builder.AddProject<Projects.TilesAndTokens_Demo>("demo")
    .WithArgs("all")
    .WithReference(database)
    .WaitFor(database);

// The Post 5 benchmark (docs/post-5-sql-and-plans.md). It empties and reseeds SavedGames, so it
// gets its own database, and only runs when started from the dashboard.
var benchDatabase = postgres.AddDatabase("bench-db", databaseName: "tilesandtokens_bench");

builder.AddProject<Projects.TilesAndTokens_Demo>("bench")
    .WithArgs("bench")
    .WithReference(benchDatabase, connectionName: "tilesandtokens")
    .WaitFor(benchDatabase)
    .WithExplicitStart();

builder.Build().Run();
