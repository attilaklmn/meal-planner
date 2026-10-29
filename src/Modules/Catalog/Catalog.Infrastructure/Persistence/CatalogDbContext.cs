using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public const string Schema = "catalog";

    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    // Keeps this module's migration history next to its tables, so modules migrate independently.
    internal static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schema);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
