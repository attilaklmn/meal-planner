using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

internal sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    // numeric(8,3): up to 99999.999, enough for kcal per 100 g, and three decimals as in USDA data.
    private const int NutrientPrecision = 8;
    private const int NutrientScale = 3;

    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.HasKey(i => i.Id);
        // The domain creates the id (Guid v7); the database never generates one.
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Name).HasMaxLength(Ingredient.NameMaxLength);
        builder.Property(i => i.HungarianName).HasMaxLength(Ingredient.NameMaxLength);
        builder.Property(i => i.Source).HasMaxLength(Ingredient.SourceMaxLength);
        builder.Property(i => i.SourceId).HasMaxLength(Ingredient.SourceIdMaxLength);

        // The importer matches rows by their id in the source dataset.
        builder.HasIndex(i => new { i.Source, i.SourceId }).IsUnique();

        // Complex type: the value object's fields become columns of this table, with no key or identity of its own.
        builder.ComplexProperty(i => i.NutrientsPer100g, nutrients =>
        {
            nutrients.Property(n => n.EnergyKcal).HasPrecision(NutrientPrecision, NutrientScale);
            nutrients.Property(n => n.Protein).HasPrecision(NutrientPrecision, NutrientScale);
            nutrients.Property(n => n.Carbohydrate).HasPrecision(NutrientPrecision, NutrientScale);
            nutrients.Property(n => n.Fat).HasPrecision(NutrientPrecision, NutrientScale);
            nutrients.Property(n => n.Fiber).HasPrecision(NutrientPrecision, NutrientScale);
        });
    }
}
