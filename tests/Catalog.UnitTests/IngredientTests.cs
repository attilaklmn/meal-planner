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

    [Fact]
    public void UpdateFromSource_changes_data_but_keeps_identity()
    {
        var egg = Ingredient.Create("Egg", "Tojás", EggPer100g, "usda", "748967");
        var id = egg.Id;
        var corrected = new NutrientProfile(148m, 12.4m, 1m, 10m, 0m);

        egg.UpdateFromSource(" Egg, whole, raw ", " Tyúktojás ", corrected, densityGramsPerMl: 1.027m, pieceWeightGrams: 50m);

        Assert.Equal(id, egg.Id);
        Assert.Equal("usda", egg.Source);
        Assert.Equal("748967", egg.SourceId);
        Assert.Equal("Egg, whole, raw", egg.Name);
        Assert.Equal("Tyúktojás", egg.HungarianName);
        Assert.Equal(corrected, egg.NutrientsPer100g);
        Assert.Equal(1.027m, egg.DensityGramsPerMl);
        Assert.Equal(50m, egg.PieceWeightGrams);
    }

    [Fact]
    public void UpdateFromSource_with_invalid_data_leaves_the_ingredient_untouched()
    {
        var egg = Ingredient.Create("Egg", "Tojás", EggPer100g, "usda", "1");
        var impossible = new NutrientProfile(500m, 40m, 40m, 30m, 0m);

        Assert.Throws<ArgumentException>(() => egg.UpdateFromSource("Egg, renamed", "Tojás", impossible, null, null));

        Assert.Equal("Egg", egg.Name);
        Assert.Equal(EggPer100g, egg.NutrientsPer100g);
    }

    [Fact]
    public void Unit_data_is_optional()
    {
        var flour = Ingredient.Create("Wheat flour", "Búzaliszt", EggPer100g, "usda", "1", densityGramsPerMl: 0.528m);

        Assert.Equal(0.528m, flour.DensityGramsPerMl);
        Assert.Null(flour.PieceWeightGrams);
    }

    [Theory]
    [InlineData(0.0, null)]
    [InlineData(-1.0, null)]
    [InlineData(3.5, null)]
    [InlineData(null, 0.0)]
    [InlineData(null, -50.0)]
    public void Rejects_implausible_unit_data(double? density, double? pieceWeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Ingredient.Create("Egg", "Tojás", EggPer100g, "usda", "1", (decimal?)density, (decimal?)pieceWeight));
    }

    [Fact]
    public void UpdateFromSource_clears_unit_data_the_source_no_longer_has()
    {
        var egg = Ingredient.Create("Egg", "Tojás", EggPer100g, "usda", "1", pieceWeightGrams: 50m);

        egg.UpdateFromSource("Egg", "Tojás", EggPer100g, densityGramsPerMl: null, pieceWeightGrams: null);

        Assert.Null(egg.PieceWeightGrams);
    }

    [Fact]
    public void UpdateFromSource_with_invalid_unit_data_leaves_the_ingredient_untouched()
    {
        var egg = Ingredient.Create("Egg", "Tojás", EggPer100g, "usda", "1", pieceWeightGrams: 50m);

        Assert.Throws<ArgumentOutOfRangeException>(() => egg.UpdateFromSource("Egg, renamed", "Tojás", EggPer100g, null, -1m));

        Assert.Equal("Egg", egg.Name);
        Assert.Equal(50m, egg.PieceWeightGrams);
    }
}
