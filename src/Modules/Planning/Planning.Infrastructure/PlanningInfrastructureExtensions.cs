using Planning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Planning.Infrastructure;

public static class PlanningInfrastructureExtensions
{
    public static IHostApplicationBuilder AddPlanningInfrastructure(this IHostApplicationBuilder builder, string connectionName)
    {
        // Registered by hand (not AddNpgsqlDbContext) so the migrations history table can live in the module's schema.
        // The connection string is read when a DbContext is created, not here: build-time OpenAPI generation
        // runs this startup code without a database configured.
        builder.Services.AddDbContext<PlanningDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString(connectionName)
                ?? throw new InvalidOperationException($"Connection string '{connectionName}' is missing.");

            options.UseNpgsql(connectionString, PlanningDbContext.ConfigureNpgsql);
        });

        // Adds Aspire's health check, OpenTelemetry instrumentation and retry-on-failure to the registration above.
        builder.EnrichNpgsqlDbContext<PlanningDbContext>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddHostedService<PlanningMigrationService>();
        }

        return builder;
    }
}
