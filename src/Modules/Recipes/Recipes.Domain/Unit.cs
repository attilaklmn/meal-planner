namespace Recipes.Domain;

/// <summary>Units a recipe can measure an ingredient in.</summary>
public enum Unit
{
    Gram,
    Kilogram,
    Milliliter,
    Liter,
    Teaspoon,
    Tablespoon,
    Cup,
    Piece,
}

/// <summary>What a unit measures; only quantities of the same dimension convert into each other without ingredient data.</summary>
public enum UnitDimension
{
    Mass,
    Volume,
    Count,
}

public static class UnitExtensions
{
    public static UnitDimension Dimension(this Unit unit) => unit switch
    {
        Unit.Gram or Unit.Kilogram => UnitDimension.Mass,
        Unit.Milliliter or Unit.Liter or Unit.Teaspoon or Unit.Tablespoon or Unit.Cup => UnitDimension.Volume,
        Unit.Piece => UnitDimension.Count,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit."),
    };

    /// <summary>
    /// How many base units (g, ml or pieces) one of this unit is.
    /// Spoons and cups are the metric kitchen measures: 5 ml, 15 ml and 250 ml (close to a Hungarian "bögre").
    /// </summary>
    public static decimal BaseFactor(this Unit unit) => unit switch
    {
        Unit.Gram => 1m,
        Unit.Kilogram => 1000m,
        Unit.Milliliter => 1m,
        Unit.Liter => 1000m,
        Unit.Teaspoon => 5m,
        Unit.Tablespoon => 15m,
        Unit.Cup => 250m,
        Unit.Piece => 1m,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit."),
    };

    /// <summary>The unit a dimension's base factor is expressed in.</summary>
    public static Unit BaseUnit(this UnitDimension dimension) => dimension switch
    {
        UnitDimension.Mass => Unit.Gram,
        UnitDimension.Volume => Unit.Milliliter,
        UnitDimension.Count => Unit.Piece,
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown dimension."),
    };
}
