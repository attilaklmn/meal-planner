using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planning.Domain;

namespace Planning.Infrastructure.Persistence;

internal sealed class MealPlanEntryConfiguration : IEntityTypeConfiguration<MealPlanEntry>
{
    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable("MealPlanEntries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        // Stored by name ("Wednesday", "Lunch"): readable in SQL, and reordering an enum can't corrupt data.
        builder.Property(e => e.Day).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.Meal).HasConversion<string>().HasMaxLength(10);

        // The copied recipe data lives in the entry's row (ADR 0005). No foreign key: the recipe is in another module's
        // schema (another service in v2), and a deleted recipe stays on the plan under its copied title.
        builder.ComplexProperty(e => e.Recipe, recipe =>
        {
            // Mapped explicitly: the convention skips get-only properties.
            recipe.Property(r => r.RecipeId);
            recipe.Property(r => r.Title).HasMaxLength(RecipeSnapshot.TitleMaxLength);
            // Unrounded, as Recipes calculates it; four decimals keep sums over a week exact enough.
            recipe.Property(r => r.EnergyKcalPerServing).HasPrecision(10, 4);
        });

        builder.Ignore(e => e.EnergyKcal);
    }
}
