using Recipes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Recipes.Infrastructure;

public static class RecipesInfrastructureExtensions
{
    public static IHostApplicationBuilder AddRecipesInfrastructure(this IHostApplicationBuilder builder, string connectionName)
    {
        // Registered by hand (not AddNpgsqlDbContext) so the migrations history table can live in the module's schema.
        // The connection string is read when a DbContext is created, not here: build-time OpenAPI generation
        // runs this startup code without a database configured.
        builder.Services.AddDbContext<RecipesDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString(connectionName)
                ?? throw new InvalidOperationException($"Connection string '{connectionName}' is missing.");

            options.UseNpgsql(connectionString, RecipesDbContext.ConfigureNpgsql);
        });

        // Adds Aspire's health check, OpenTelemetry instrumentation and retry-on-failure to the registration above.
        builder.EnrichNpgsqlDbContext<RecipesDbContext>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddHostedService<RecipesMigrationService>();
        }

        return builder;
    }
}
