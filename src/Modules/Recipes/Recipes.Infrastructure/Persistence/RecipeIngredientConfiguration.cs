using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain;

namespace Recipes.Infrastructure.Persistence;

internal sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("RecipeIngredients");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Note).HasMaxLength(RecipeIngredient.NoteMaxLength);

        // A recipe's lines are always read in order. Not unique: renumbering after a removal would collide mid-update.
        builder.HasIndex("RecipeId", nameof(RecipeIngredient.Position));

        // The other side of the many-to-many: a line points at Recipes' copy of the ingredient. Restrict, because
        // a copy that recipes still use must not disappear (v2's events update copies, they never delete them).
        builder.HasOne<CatalogIngredient>()
            .WithMany()
            .HasForeignKey(i => i.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ComplexProperty(i => i.Quantity, quantity =>
        {
            // numeric(10,3): up to 9999999.999, three decimals for amounts like 0.125 kg.
            quantity.Property(q => q.Amount).HasPrecision(10, 3);
            // Stored by name ("Tablespoon"): readable in SQL, and reordering the enum can't corrupt data.
            quantity.Property(q => q.Unit).HasConversion<string>().HasMaxLength(20);
        });
    }
}
