namespace Catalog.Contracts;

/// <summary>
/// An ingredient as other modules see it: names, nutrition per 100 g (kcal, grams) and unit data.
/// Flat primitives, no Catalog domain types, so consumers map it to their own model.
/// </summary>
public sealed record CatalogIngredientData(
    Guid Id,
    string Name,
    string HungarianName,
    decimal EnergyKcalPer100g,
    decimal ProteinPer100g,
    decimal CarbohydratePer100g,
    decimal FatPer100g,
    decimal FiberPer100g,
    decimal? DensityGramsPerMl,
    decimal? PieceWeightGrams);
