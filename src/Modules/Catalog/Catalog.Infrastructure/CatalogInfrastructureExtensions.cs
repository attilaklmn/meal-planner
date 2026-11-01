using Catalog.Contracts;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Catalog.Infrastructure;

public static class CatalogInfrastructureExtensions
{
    public static IHostApplicationBuilder AddCatalogInfrastructure(this IHostApplicationBuilder builder, string connectionName)
    {
        // Registered by hand (not AddNpgsqlDbContext) so the migrations history table can live in the module's schema.
        // The connection string is read when a DbContext is created, not here: build-time OpenAPI generation
        // runs this startup code without a database configured.
        builder.Services.AddDbContext<CatalogDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString(connectionName)
                ?? throw new InvalidOperationException($"Connection string '{connectionName}' is missing.");

            options.UseNpgsql(connectionString, CatalogDbContext.ConfigureNpgsql);
        });

        // Adds Aspire's health check, OpenTelemetry instrumentation and retry-on-failure to the registration above.
        builder.EnrichNpgsqlDbContext<CatalogDbContext>();

        // Catalog's public API for other modules (Catalog.Contracts).
        builder.Services.AddScoped<ICatalogIngredients, CatalogIngredients>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddScoped<IngredientSeeder>();
            builder.Services.AddHostedService<CatalogDatabaseInitializer>();
        }

        return builder;
    }
}
