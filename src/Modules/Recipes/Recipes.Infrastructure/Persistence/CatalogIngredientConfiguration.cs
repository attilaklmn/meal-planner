using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain;

namespace Recipes.Infrastructure.Persistence;

internal sealed class CatalogIngredientConfiguration : IEntityTypeConfiguration<CatalogIngredient>
{
    // numeric(8,3), as in Catalog's table, so the copy holds exactly what Catalog has.
    private const int NutrientPrecision = 8;
    private const int NutrientScale = 3;

    public void Configure(EntityTypeBuilder<CatalogIngredient> builder)
    {
        // recipes."Ingredients": the same name as Catalog's table, in this module's schema.
        builder.ToTable("Ingredients");

        builder.HasKey(i => i.Id);
        // Catalog's id: never generated here.
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Name).HasMaxLength(CatalogIngredient.NameMaxLength);
        builder.Property(i => i.HungarianName).HasMaxLength(CatalogIngredient.NameMaxLength);

        builder.ComplexProperty(i => i.NutrientsPer100g, nutrients =>
        {
            nutrients.Property(n => n.EnergyKcal).HasPrecision(NutrientPrecision, NutrientScale);
            nutrients.Property(n => n.Protein).HasPrecision(NutrientPrecision, NutrientScale);
            nutrients.Property(n => n.Carbohydrate).HasPrecision(NutrientPrecision, NutrientScale);
            nutrients.Property(n => n.Fat).HasPrecision(NutrientPrecision, NutrientScale);
            nutrients.Property(n => n.Fiber).HasPrecision(NutrientPrecision, NutrientScale);
        });

        // Same column names as in Catalog; the value object is a grouping in code, not in the table.
        builder.ComplexProperty(i => i.UnitConversion, conversion =>
        {
            conversion.Property(c => c.DensityGramsPerMl)
                .HasColumnName(nameof(UnitConversion.DensityGramsPerMl))
                .HasPrecision(NutrientPrecision, NutrientScale);
            conversion.Property(c => c.PieceWeightGrams)
                .HasColumnName(nameof(UnitConversion.PieceWeightGrams))
                .HasPrecision(NutrientPrecision, NutrientScale);
        });
    }
}
