using Recipes.Contracts;
using Recipes.Domain;
using Recipes.Infrastructure;
using static Recipes.Infrastructure.RecipeReader;

namespace Recipes.UnitTests;

public class RecipeReaderTests
{
    private static readonly CatalogIngredient Flour = CatalogIngredient.Create(
        Guid.CreateVersion7(), "Flour", "Búzaliszt", new NutrientProfile(364m, 10.33m, 76.31m, 0.98m, 2.7m), new UnitConversion(0.528m, null));

    private static readonly CatalogIngredient Oil = CatalogIngredient.Create(
        Guid.CreateVersion7(), "Olive oil", "Olívaolaj", new NutrientProfile(884m, 0m, 0m, 100m, 0m), new UnitConversion(0.913m, null));

    private static readonly CatalogIngredient Egg = CatalogIngredient.Create(
        Guid.CreateVersion7(), "Egg", "Tojás", new NutrientProfile(143m, 12.56m, 0.72m, 9.51m, 0m), new UnitConversion(1.027m, 50m));

    private static readonly Dictionary<Guid, CatalogIngredient> Ingredients = new()
    {
        [Flour.Id] = Flour,
        [Oil.Id] = Oil,
        [Egg.Id] = Egg,
    };

    private static RecipeRow Nokedli() => new(
        Guid.CreateVersion7(),
        "Nokedli",
        2,
        [
            new LineRow(Flour.Id, 250m, Unit.Gram),
            new LineRow(Oil.Id, 1m, Unit.Tablespoon),
            new LineRow(Egg.Id, 2m, Unit.Piece),
        ]);

    [Fact]
    public void Lines_carry_the_grams_of_their_quantity()
    {
        var recipe = ToRecipeData(Nokedli(), Ingredients);

        Assert.Equal(
            [
                new RecipeIngredientData(Flour.Id, "Búzaliszt", 250m, IngredientUnit.Gram, 250m),
                // 15 ml × 0.913 g/ml
                new RecipeIngredientData(Oil.Id, "Olívaolaj", 1m, IngredientUnit.Tablespoon, 13.695m),
                // 2 × 50 g
                new RecipeIngredientData(Egg.Id, "Tojás", 2m, IngredientUnit.Piece, 100m),
            ],
            recipe.Ingredients);
    }

    [Fact]
    public void Energy_is_per_serving_as_the_domain_calculates_it()
    {
        var row = Nokedli();

        var recipe = ToRecipeData(row, Ingredients);

        // (910 + 121.0638 + 143) / 2 servings
        Assert.Equal(587.0319m, recipe.EnergyKcalPerServing);
        Assert.Equal((row.Id, "Nokedli", 2), (recipe.Id, recipe.Title, recipe.Servings));
    }

    [Fact]
    public void Every_domain_unit_has_a_contract_unit_of_the_same_name()
    {
        foreach (var unit in Enum.GetValues<Unit>())
        {
            Assert.Equal(unit.ToString(), ToIngredientUnit(unit).ToString());
        }

        Assert.Equal(Enum.GetValues<Unit>().Length, Enum.GetValues<IngredientUnit>().Length);
    }
}
