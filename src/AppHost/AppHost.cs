var builder = DistributedApplication.CreateBuilder(args);

// One Postgres server and one database; each module owns its own schema inside it.
// Persistent lifetime keeps the container running between AppHost restarts, so startup stays fast.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("mealplanner");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

// Vite dev server; WithReference passes the Api's URL as env vars, which vite.config.ts uses as proxy target.
builder.AddViteApp("web", "../web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
