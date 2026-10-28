using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Catalog.Infrastructure;

public static class CatalogInfrastructureExtensions
{
    public static IHostApplicationBuilder AddCatalogInfrastructure(this IHostApplicationBuilder builder, string connectionName)
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Connection string '{connectionName}' is missing.");

        // Registered by hand (not AddNpgsqlDbContext) so the migrations history table can live in the module's schema.
        builder.Services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString, CatalogDbContext.ConfigureNpgsql));

        // Adds Aspire's health check, OpenTelemetry instrumentation and retry-on-failure to the registration above.
        builder.EnrichNpgsqlDbContext<CatalogDbContext>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddHostedService<CatalogMigrationService>();
        }

        return builder;
    }
}
