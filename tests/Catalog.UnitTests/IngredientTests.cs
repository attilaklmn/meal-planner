using Catalog.Domain;

namespace Catalog.UnitTests;

public class IngredientTests
{
    private static readonly NutrientProfile EggPer100g = new(143m, 12.6m, 0.7m, 9.5m, 0m);

    [Fact]
    public void Create_sets_every_field_and_trims_text()
    {
        var egg = Ingredient.Create(" Egg, whole, raw ", " Tojás ", EggPer100g, "usda", "748967");

        Assert.NotEqual(Guid.Empty, egg.Id);
        Assert.Equal("Egg, whole, raw", egg.Name);
        Assert.Equal("Tojás", egg.HungarianName);
        Assert.Equal(EggPer100g, egg.NutrientsPer100g);
        Assert.Equal("usda", egg.Source);
        Assert.Equal("748967", egg.SourceId);
    }

    [Fact]
    public void Create_gives_each_ingredient_its_own_id()
    {
        var first = Ingredient.Create("Egg", "Tojás", EggPer100g, "usda", "1");
        var second = Ingredient.Create("Egg", "Tojás", EggPer100g, "usda", "2");

        Assert.NotEqual(first.Id, second.Id);
    }

    [Theory]
    [InlineData("", "Tojás", "usda", "1")]
    [InlineData("Egg", " ", "usda", "1")]
    [InlineData("Egg", "Tojás", "", "1")]
    [InlineData("Egg", "Tojás", "usda", "")]
    public void Create_rejects_blank_text(string name, string hungarianName, string source, string sourceId)
    {
        Assert.ThrowsAny<ArgumentException>(() => Ingredient.Create(name, hungarianName, EggPer100g, source, sourceId));
    }

    [Fact]
    public void Create_rejects_too_long_name()
    {
        var name = new string('a', Ingredient.NameMaxLength + 1);

        var exception = Assert.Throws<ArgumentException>(() => Ingredient.Create(name, "Tojás", EggPer100g, "usda", "1"));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_more_macros_than_fit_in_100g()
    {
        var impossible = new NutrientProfile(500m, 40m, 40m, 30m, 0m);

        var exception = Assert.Throws<ArgumentException>(() => Ingredient.Create("Impossible", "Lehetetlen", impossible, "usda", "1"));
        Assert.Equal("nutrientsPer100g", exception.ParamName);
    }

    [Fact]
    public void Create_tolerates_rounding_in_source_data()
    {
        var oil = new NutrientProfile(884m, 0.1m, 0m, 100m, 0m);

        var ingredient = Ingredient.Create("Oil, olive", "Olívaolaj", oil, "usda", "1");

        Assert.Equal(100.1m, ingredient.NutrientsPer100g.Macros);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(50, 71.5, 6.3)]
    [InlineData(100, 143, 12.6)]
    [InlineData(250, 357.5, 31.5)]
    public void NutrientsFor_scales_from_100g(decimal grams, decimal expectedKcal, decimal expectedProtein)
    {
        var egg = Ingredient.Create("Egg", "Tojás", EggPer100g, "usda", "1");

        var nutrients = egg.NutrientsFor(grams);

        Assert.Equal(expectedKcal, nutrients.EnergyKcal);
        Assert.Equal(expectedProtein, nutrients.Protein);
    }

    [Fact]
    public void NutrientsFor_rejects_negative_amount()
    {
        var egg = Ingredient.Create("Egg", "Tojás", EggPer100g, "usda", "1");

        Assert.Throws<ArgumentOutOfRangeException>(() => egg.NutrientsFor(-1));
    }
}
