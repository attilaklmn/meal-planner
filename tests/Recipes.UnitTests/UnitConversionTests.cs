using Recipes.Domain;

namespace Recipes.UnitTests;

public class UnitConversionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void Rejects_non_positive_density(decimal density)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnitConversion(density, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Rejects_non_positive_piece_weight(decimal pieceWeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnitConversion(null, pieceWeight));
    }

    [Fact]
    public void None_converts_only_mass()
    {
        Assert.Equal(1m, UnitConversion.None.GramsPerBaseUnit(UnitDimension.Mass));
        Assert.Null(UnitConversion.None.GramsPerBaseUnit(UnitDimension.Volume));
        Assert.Null(UnitConversion.None.GramsPerBaseUnit(UnitDimension.Count));
    }

    [Theory]
    [InlineData(Unit.Gram, UnitDimension.Mass)]
    [InlineData(Unit.Kilogram, UnitDimension.Mass)]
    [InlineData(Unit.Milliliter, UnitDimension.Volume)]
    [InlineData(Unit.Liter, UnitDimension.Volume)]
    [InlineData(Unit.Teaspoon, UnitDimension.Volume)]
    [InlineData(Unit.Tablespoon, UnitDimension.Volume)]
    [InlineData(Unit.Cup, UnitDimension.Volume)]
    [InlineData(Unit.Piece, UnitDimension.Count)]
    public void Every_unit_has_a_dimension(Unit unit, UnitDimension expected)
    {
        Assert.Equal(expected, unit.Dimension());
    }
}
