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

builder.Build().Run();
