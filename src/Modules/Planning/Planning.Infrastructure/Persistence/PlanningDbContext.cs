using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Planning.Domain;

namespace Planning.Infrastructure.Persistence;

public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options) : DbContext(options)
{
    public const string Schema = "planning";

    /// <summary>The unique index that allows one plan per week.</summary>
    internal const string WeekStartIndex = "IX_MealPlans_WeekStart";

    public DbSet<MealPlan> MealPlans => Set<MealPlan>();

    /// <summary>Whether saving failed because another request created the same week's plan first.</summary>
    public static bool IsDuplicateWeek(DbUpdateException exception) =>
        exception?.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: WeekStartIndex,
        };

    // Keeps this module's migration history next to its tables, so modules migrate independently.
    internal static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schema);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanningDbContext).Assembly);
    }
}
