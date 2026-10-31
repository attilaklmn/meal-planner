using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain;

namespace Recipes.Infrastructure.Persistence;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.HasKey(r => r.Id);
        // The domain creates the id (Guid v7); the database never generates one.
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Title).HasMaxLength(Recipe.TitleMaxLength);

        // Steps are read and written only with their recipe, in order, and never queried one by one:
        // a document, so a jsonb column instead of a table.
        builder.Ignore(r => r.Steps);
        builder.ComplexCollection<List<RecipeStep>, RecipeStep>("_steps", steps =>
        {
            steps.ToJson(nameof(Recipe.Steps));
            // Mapped explicitly: the convention skips get-only properties inside a JSON collection.
            steps.Property(s => s.Text);
        });

        // Tags are filtered on ("leves"), so they get a native text[] (EF's default for a primitive collection
        // on PostgreSQL) with a GIN index, which serves `tags @> '{leves}'` without scanning every recipe.
        builder.Property(r => r.Tags);
        builder.HasIndex(r => r.Tags).HasMethod("gin");

        // An explicit join entity with payload, reached only through the aggregate's backing field.
        builder.HasMany(r => r.Ingredients)
            .WithOne()
            .HasForeignKey("RecipeId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Ingredients).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
