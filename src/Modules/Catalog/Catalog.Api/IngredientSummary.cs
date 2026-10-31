namespace Catalog.Api;

/// <summary>
/// An ingredient as search results show it, with nutrition per 100 g, and the unit data that tells which units
/// it can be measured in: volumes need <paramref name="DensityGramsPerMl"/>, pieces need <paramref name="PieceWeightGrams"/>.
/// </summary>
public sealed record IngredientSummary(
    Guid Id,
    string Name,
    string HungarianName,
    NutrientsResponse NutrientsPer100g,
    decimal? DensityGramsPerMl,
    decimal? PieceWeightGrams);

/// <summary>Energy in kcal, everything else in grams.</summary>
public sealed record NutrientsResponse(decimal EnergyKcal, decimal Protein, decimal Carbohydrate, decimal Fat, decimal Fiber);
