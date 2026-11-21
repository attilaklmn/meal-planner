using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planning.Domain;

namespace Planning.Infrastructure.Persistence;

internal sealed class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.HasKey(p => p.Id);
        // The domain creates the id (Guid v7); the database never generates one.
        builder.Property(p => p.Id).ValueGeneratedNever();

        // One plan per week: plans are looked up by their Monday, and two requests creating the same week's plan at once
        // fail on this index instead of leaving two plans. A surrogate id rather than the date as key, because with
        // households (M5) the week alone stops being unique.
        builder.HasIndex(p => p.WeekStart).IsUnique();

        // Entries are a child entity, reached only through the aggregate's backing field.
        builder.HasMany(p => p.Entries)
            .WithOne()
            .HasForeignKey("MealPlanId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.WeeklyEnergyKcal);
    }
}
