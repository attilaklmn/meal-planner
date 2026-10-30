namespace Catalog.Api;

/// <summary>An ingredient as search results show it, with nutrition per 100 g.</summary>
public sealed record IngredientSummary(Guid Id, string Name, string HungarianName, NutrientsResponse NutrientsPer100g);

/// <summary>Energy in kcal, everything else in grams.</summary>
public sealed record NutrientsResponse(decimal EnergyKcal, decimal Protein, decimal Carbohydrate, decimal Fat, decimal Fiber);
