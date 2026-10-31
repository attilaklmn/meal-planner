namespace Recipes.Domain;

/// <summary>
/// The ingredient-specific data that turns volumes and pieces into grams: density (g/ml) and the weight of one piece (g).
/// Either can be missing; then quantities in that dimension can't be converted for this ingredient.
/// </summary>
public sealed record UnitConversion
{
    /// <summary>For ingredients without unit data: only mass units convert.</summary>
    public static readonly UnitConversion None = new(null, null);

    public UnitConversion(decimal? densityGramsPerMl, decimal? pieceWeightGrams)
    {
        if (densityGramsPerMl is { } density)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(density, nameof(densityGramsPerMl));
        }

        if (pieceWeightGrams is { } pieceWeight)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pieceWeight, nameof(pieceWeightGrams));
        }

        DensityGramsPerMl = densityGramsPerMl;
        PieceWeightGrams = pieceWeightGrams;
    }

    // Get-only (no init), like NutrientProfile: a `with` expression would skip the validation.
    public decimal? DensityGramsPerMl { get; }

    public decimal? PieceWeightGrams { get; }

    /// <summary>Grams per base unit of the dimension (g, ml or piece), or null when this ingredient lacks the data.</summary>
    public decimal? GramsPerBaseUnit(UnitDimension dimension) => dimension switch
    {
        UnitDimension.Mass => 1m,
        UnitDimension.Volume => DensityGramsPerMl,
        UnitDimension.Count => PieceWeightGrams,
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown dimension."),
    };
}
