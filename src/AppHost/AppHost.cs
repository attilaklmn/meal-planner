var builder = DistributedApplication.CreateBuilder(args);

// One Postgres server and one database; each module owns its own schema inside it.
// Persistent lifetime keeps the container running between AppHost restarts, so startup stays fast.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("mealplanner");

builder.AddProject<Projects.Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
