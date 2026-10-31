using Recipes.Domain;

namespace Recipes.UnitTests;

public class QuantityTests
{
    private static readonly UnitConversion OliveOil = new(densityGramsPerMl: 0.92m, pieceWeightGrams: null);
    private static readonly UnitConversion Egg = new(densityGramsPerMl: null, pieceWeightGrams: 50m);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_non_positive_amount(int amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(amount, Unit.Gram));
    }

    [Fact]
    public void Rejects_unknown_unit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(1m, (Unit)99));
    }

    [Theory]
    [InlineData(Unit.Gram, 250, 250)]
    [InlineData(Unit.Kilogram, 0.5, 500)]
    public void Mass_converts_without_ingredient_data(Unit unit, decimal amount, decimal expectedGrams)
    {
        Assert.Equal(expectedGrams, new Quantity(amount, unit).ToGrams(UnitConversion.None));
    }

    [Fact]
    public void Two_tablespoons_of_olive_oil_convert_through_density()
    {
        // 2 × 15 ml × 0.92 g/ml
        Assert.Equal(27.6m, new Quantity(2m, Unit.Tablespoon).ToGrams(OliveOil));
    }

    [Theory]
    [InlineData(Unit.Milliliter, 100, 92)]
    [InlineData(Unit.Liter, 0.5, 460)]
    [InlineData(Unit.Teaspoon, 1, 4.6)]
    [InlineData(Unit.Cup, 1, 230)]
    public void Every_volume_unit_converts_through_density(Unit unit, decimal amount, decimal expectedGrams)
    {
        Assert.Equal(expectedGrams, new Quantity(amount, unit).ToGrams(OliveOil));
    }

    [Fact]
    public void Three_eggs_convert_through_piece_weight()
    {
        Assert.Equal(150m, new Quantity(3m, Unit.Piece).ToGrams(Egg));
    }

    [Fact]
    public void Volume_without_density_fails_clearly()
    {
        var oneCupOfFlour = new Quantity(1m, Unit.Cup);

        var error = Assert.Throws<InvalidOperationException>(() => oneCupOfFlour.ToGrams(UnitConversion.None));

        Assert.Contains("no density", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Piece_without_piece_weight_fails_clearly()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new Quantity(2m, Unit.Piece).ToGrams(OliveOil));

        Assert.Contains("no piece weight", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Unit.Gram, true)]
    [InlineData(Unit.Tablespoon, true)]
    [InlineData(Unit.Piece, false)]
    public void CanConvertToGrams_tells_in_advance(Unit unit, bool expected)
    {
        Assert.Equal(expected, new Quantity(1m, unit).CanConvertToGrams(OliveOil));
    }

    [Fact]
    public void Scale_keeps_the_unit()
    {
        Assert.Equal(new Quantity(1m, Unit.Tablespoon), new Quantity(2m, Unit.Tablespoon).Scale(0.5m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Scale_rejects_non_positive_factor(int factor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(2m, Unit.Tablespoon).Scale(factor));
    }

    [Fact]
    public void Adding_the_same_unit_keeps_it()
    {
        Assert.Equal(new Quantity(2m, Unit.Cup), new Quantity(1m, Unit.Cup) + new Quantity(1m, Unit.Cup));
    }

    [Fact]
    public void Adding_mixed_units_of_one_dimension_gives_the_base_unit()
    {
        Assert.Equal(new Quantity(115m, Unit.Milliliter), new Quantity(1m, Unit.Tablespoon) + new Quantity(100m, Unit.Milliliter));
        Assert.Equal(new Quantity(1200m, Unit.Gram), new Quantity(1m, Unit.Kilogram) + new Quantity(200m, Unit.Gram));
    }

    [Fact]
    public void Adding_different_dimensions_fails()
    {
        Assert.Throws<InvalidOperationException>(() => new Quantity(3m, Unit.Piece) + new Quantity(200m, Unit.Gram));
    }

    [Fact]
    public void Equality_is_by_representation()
    {
        Assert.Equal(new Quantity(1m, Unit.Kilogram), new Quantity(1m, Unit.Kilogram));
        Assert.NotEqual(new Quantity(1m, Unit.Kilogram), new Quantity(1000m, Unit.Gram));
    }
}
