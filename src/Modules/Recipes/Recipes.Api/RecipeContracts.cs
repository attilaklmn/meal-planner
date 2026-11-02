using Recipes.Domain;

namespace Recipes.Api;

/// <summary>A whole recipe, as created (POST) or replaced (PUT). Ingredient lines are in display order.</summary>
public sealed record RecipeRequest(
    string Title,
    int Servings,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Tags,
    IReadOnlyList<RecipeIngredientRequest> Ingredients);

/// <summary>One ingredient line: a Catalog ingredient's id, an amount in a unit, and an optional note.</summary>
public sealed record RecipeIngredientRequest(Guid IngredientId, decimal Amount, Unit Unit, string? Note = null);

/// <summary>A recipe in the list, with energy per serving in kcal.</summary>
public sealed record RecipeSummary(Guid Id, string Title, int Servings, IReadOnlyList<string> Tags, decimal EnergyKcalPerServing);

/// <summary>A recipe with its ingredient lines, steps and nutrition, for its own page.</summary>
public sealed record RecipeDetails(
    Guid Id,
    string Title,
    int Servings,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Tags,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    NutritionResponse NutritionPerServing,
    NutritionResponse NutritionTotal);

/// <summary>
/// One ingredient line, with the ingredient's data from Recipes' copy: names, and the nutrients per 100 g and unit data
/// the editor needs to recalculate nutrition and to know which units the ingredient can be measured in.
/// </summary>
public sealed record RecipeIngredientResponse(
    Guid Id,
    Guid IngredientId,
    string Name,
    string HungarianName,
    decimal Amount,
    Unit Unit,
    string? Note,
    NutritionResponse NutrientsPer100g,
    decimal? DensityGramsPerMl,
    decimal? PieceWeightGrams);

/// <summary>Energy in kcal, everything else in grams, rounded to 2 decimals.</summary>
public sealed record NutritionResponse(decimal EnergyKcal, decimal Protein, decimal Carbohydrate, decimal Fat, decimal Fiber)
{
    internal const int Decimals = 2;

    internal static NutritionResponse From(NutrientProfile nutrients) => new(
        Math.Round(nutrients.EnergyKcal, Decimals),
        Math.Round(nutrients.Protein, Decimals),
        Math.Round(nutrients.Carbohydrate, Decimals),
        Math.Round(nutrients.Fat, Decimals),
        Math.Round(nutrients.Fiber, Decimals));
}
