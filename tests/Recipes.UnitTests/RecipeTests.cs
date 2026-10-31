using Recipes.Domain;

namespace Recipes.UnitTests;

public class RecipeTests
{
    // Per 100 g, rounded from USDA SR Legacy.
    private static readonly CatalogIngredient Flour = CatalogIngredient.Create(
        Guid.CreateVersion7(), "Wheat flour, white, all-purpose", "Búzaliszt",
        new NutrientProfile(364m, 10.33m, 76.31m, 0.98m, 2.7m), new UnitConversion(0.528m, null));

    private static readonly CatalogIngredient OliveOil = CatalogIngredient.Create(
        Guid.CreateVersion7(), "Oil, olive", "Olívaolaj",
        new NutrientProfile(884m, 0m, 0m, 100m, 0m), new UnitConversion(0.913m, null));

    private static readonly CatalogIngredient Egg = CatalogIngredient.Create(
        Guid.CreateVersion7(), "Egg, whole, raw, fresh", "Tojás",
        new NutrientProfile(143m, 12.56m, 0.72m, 9.51m, 0m), new UnitConversion(1.027m, 50m));

    private static readonly Dictionary<Guid, CatalogIngredient> Catalog = new[] { Flour, OliveOil, Egg }.ToDictionary(i => i.Id);

    [Fact]
    public void Three_ingredients_in_different_units_give_correct_nutrition_per_serving()
    {
        var recipe = Recipe.Create("Nokedli", servings: 4, steps: ["Mix", "Boil"], tags: []);
        recipe.AddIngredient(Flour, new Quantity(250m, Unit.Gram), note: null);
        recipe.AddIngredient(OliveOil, new Quantity(2m, Unit.Tablespoon), note: null);
        recipe.AddIngredient(Egg, new Quantity(3m, Unit.Piece), note: null);

        var nutrition = recipe.CalculateNutrition(Catalog);

        // 250 g flour + 2 × 15 ml × 0.913 g/ml = 27.39 g oil + 3 × 50 g = 150 g egg, then divided by 4.
        Assert.Equal(new NutrientProfile(1366.6276m, 44.665m, 191.855m, 44.105m, 6.75m), nutrition.Total);
        Assert.Equal(new NutrientProfile(341.6569m, 11.16625m, 47.96375m, 11.02625m, 1.6875m), nutrition.PerServing);
    }

    [Fact]
    public void A_recipe_without_ingredients_has_no_nutrition()
    {
        var nutrition = Recipe.Create("Water", 1, [], []).CalculateNutrition(Catalog);

        Assert.Equal(NutrientProfile.Zero, nutrition.Total);
        Assert.Equal(NutrientProfile.Zero, nutrition.PerServing);
    }

    [Fact]
    public void Nutrition_follows_the_refreshed_ingredient_copy()
    {
        var oil = CatalogIngredient.Create(
            Guid.CreateVersion7(), "Oil, olive", "Olívaolaj", new NutrientProfile(884m, 0m, 0m, 100m, 0m), new UnitConversion(1m, null));
        var recipe = Recipe.Create("Dressing", 1, [], []);
        recipe.AddIngredient(oil, new Quantity(100m, Unit.Milliliter), note: null);
        var ingredients = new Dictionary<Guid, CatalogIngredient> { [oil.Id] = oil };

        oil.RefreshFrom("Oil, olive", "Olívaolaj", new NutrientProfile(884m, 0m, 0m, 100m, 0m), new UnitConversion(0.913m, null));

        Assert.Equal(807.092m, recipe.CalculateNutrition(ingredients).Total.EnergyKcal);
    }

    [Fact]
    public void Nutrition_needs_every_ingredient_of_the_recipe()
    {
        var recipe = Recipe.Create("Omelette", 1, [], []);
        recipe.AddIngredient(Egg, new Quantity(2m, Unit.Piece), note: null);

        Assert.Throws<ArgumentException>(() => recipe.CalculateNutrition(new Dictionary<Guid, CatalogIngredient>()));
    }

    [Fact]
    public void Rejects_a_unit_the_ingredient_cant_be_measured_in()
    {
        var recipe = Recipe.Create("Bread", 1, [], []);

        var error = Assert.Throws<ArgumentException>(() => recipe.AddIngredient(Flour, new Quantity(2m, Unit.Piece), note: null));

        Assert.Contains("no piece weight", error.Message, StringComparison.Ordinal);
        Assert.Empty(recipe.Ingredients);
    }

    [Fact]
    public void The_same_ingredient_can_appear_twice()
    {
        var recipe = Recipe.Create("Pogácsa", 10, [], []);
        recipe.AddIngredient(Egg, new Quantity(1m, Unit.Piece), "for the dough");
        recipe.AddIngredient(Egg, new Quantity(1m, Unit.Piece), "for the glaze");

        Assert.Equal(["for the dough", "for the glaze"], recipe.Ingredients.Select(i => i.Note));
        // Two eggs, 100 g in all.
        Assert.Equal(143m, recipe.CalculateNutrition(Catalog).Total.EnergyKcal);
    }

    [Fact]
    public void Removing_an_ingredient_closes_the_gap_in_positions()
    {
        var recipe = Recipe.Create("Nokedli", 4, [], []);
        recipe.AddIngredient(Flour, new Quantity(250m, Unit.Gram), note: null);
        var oil = recipe.AddIngredient(OliveOil, new Quantity(1m, Unit.Tablespoon), note: null);
        recipe.AddIngredient(Egg, new Quantity(3m, Unit.Piece), note: null);

        recipe.RemoveIngredient(oil.Id);

        Assert.Equal([Flour.Id, Egg.Id], recipe.Ingredients.Select(i => i.IngredientId));
        Assert.Equal([0, 1], recipe.Ingredients.Select(i => i.Position));
    }

    [Fact]
    public void Removing_an_unknown_line_fails()
    {
        Assert.Throws<ArgumentException>(() => Recipe.Create("Soup", 2, [], []).RemoveIngredient(Guid.CreateVersion7()));
    }

    [Fact]
    public void Notes_are_trimmed_and_blank_means_none()
    {
        var recipe = Recipe.Create("Soup", 2, [], []);

        Assert.Equal("finely chopped", recipe.AddIngredient(Egg, new Quantity(1m, Unit.Piece), "  finely chopped ").Note);
        Assert.Null(recipe.AddIngredient(Egg, new Quantity(1m, Unit.Piece), "   ").Note);
    }

    [Fact]
    public void Tags_are_lowercase_and_distinct()
    {
        var recipe = Recipe.Create("Gulyásleves", 6, [], [" Leves", "leves", "Magyar "]);

        Assert.Equal(["leves", "magyar"], recipe.Tags);
    }

    [Fact]
    public void Steps_keep_their_order_and_are_trimmed()
    {
        var recipe = Recipe.Create("Tea", 1, ["  Boil water ", "Steep for 3 minutes"], []);

        Assert.Equal(["Boil water", "Steep for 3 minutes"], recipe.Steps.Select(s => s.Text));
    }

    [Theory]
    [InlineData("", 4)]
    [InlineData("   ", 4)]
    [InlineData("Soup", 0)]
    [InlineData("Soup", Recipe.MaxServings + 1)]
    public void Rejects_invalid_title_or_servings(string title, int servings)
    {
        Assert.ThrowsAny<ArgumentException>(() => Recipe.Create(title, servings, [], []));
    }

    [Fact]
    public void Rejects_blank_steps_and_tags()
    {
        Assert.ThrowsAny<ArgumentException>(() => Recipe.Create("Soup", 2, [" "], []));
        Assert.ThrowsAny<ArgumentException>(() => Recipe.Create("Soup", 2, [], [""]));
    }

    [Fact]
    public void Update_with_invalid_data_leaves_the_recipe_untouched()
    {
        var recipe = Recipe.Create("Soup", 2, ["Boil"], ["leves"]);

        Assert.ThrowsAny<ArgumentException>(() => recipe.Update("Better soup", 0, ["Boil longer"], ["leves", "téli"]));

        Assert.Equal("Soup", recipe.Title);
        Assert.Equal(2, recipe.Servings);
        Assert.Equal(["Boil"], recipe.Steps.Select(s => s.Text));
        Assert.Equal(["leves"], recipe.Tags);
    }

    [Fact]
    public void Update_replaces_title_servings_steps_and_tags()
    {
        var recipe = Recipe.Create("Soup", 2, ["Boil"], ["leves"]);

        recipe.Update("Better soup", 4, ["Boil longer"], ["téli"]);

        Assert.Equal("Better soup", recipe.Title);
        Assert.Equal(4, recipe.Servings);
        Assert.Equal(["Boil longer"], recipe.Steps.Select(s => s.Text));
        Assert.Equal(["téli"], recipe.Tags);
    }
}
