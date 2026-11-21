using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Planning.Domain;

namespace Planning.Infrastructure.Persistence;

public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options) : DbContext(options)
{
    public const string Schema = "planning";

    public DbSet<MealPlan> MealPlans => Set<MealPlan>();

    // Keeps this module's migration history next to its tables, so modules migrate independently.
    internal static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schema);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanningDbContext).Assembly);
    }
}
