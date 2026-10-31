using Recipes.Domain;

namespace Recipes.UnitTests;

public class CatalogIngredientTests
{
    private static readonly NutrientProfile EggPer100g = new(143m, 12.56m, 0.72m, 9.51m, 0m);

    [Fact]
    public void Needs_catalogs_id()
    {
        Assert.Throws<ArgumentException>(() =>
            CatalogIngredient.Create(Guid.Empty, "Egg", "Tojás", EggPer100g, UnitConversion.None));
    }

    [Fact]
    public void Rejects_blank_names()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CatalogIngredient.Create(Guid.CreateVersion7(), " ", "Tojás", EggPer100g, UnitConversion.None));
    }

    [Fact]
    public void Nutrients_for_a_quantity_go_through_its_grams()
    {
        var egg = CatalogIngredient.Create(Guid.CreateVersion7(), "Egg", "Tojás", EggPer100g, new UnitConversion(null, 50m));

        // 2 eggs = 100 g
        Assert.Equal(EggPer100g, egg.NutrientsFor(new Quantity(2m, Unit.Piece)));
    }

    [Fact]
    public void A_unit_without_conversion_data_fails_instead_of_counting_as_zero()
    {
        var egg = CatalogIngredient.Create(Guid.CreateVersion7(), "Egg", "Tojás", EggPer100g, UnitConversion.None);

        Assert.Throws<InvalidOperationException>(() => egg.NutrientsFor(new Quantity(2m, Unit.Piece)));
    }

    [Fact]
    public void RefreshFrom_with_invalid_data_leaves_the_copy_untouched()
    {
        var egg = CatalogIngredient.Create(Guid.CreateVersion7(), "Egg", "Tojás", EggPer100g, new UnitConversion(null, 50m));

        Assert.ThrowsAny<ArgumentException>(() => egg.RefreshFrom("Egg, whole", "", NutrientProfile.Zero, UnitConversion.None));

        Assert.Equal("Egg", egg.Name);
        Assert.Equal(EggPer100g, egg.NutrientsPer100g);
        Assert.Equal(50m, egg.UnitConversion.PieceWeightGrams);
    }
}
