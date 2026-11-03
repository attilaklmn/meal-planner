namespace Recipes.Contracts;

/// <summary>
/// A recipe as other modules see it: what a plan entry shows and what a shopping list is made of.
/// Flat values, no Recipes domain types, so consumers map it to their own model. Numbers are unrounded.
/// </summary>
public sealed record RecipeData(
    Guid Id,
    string Title,
    int Servings,
    decimal EnergyKcalPerServing,
    IReadOnlyList<RecipeIngredientData> Ingredients);

/// <summary>
/// One ingredient line for the recipe's <see cref="RecipeData.Servings"/>, in the recipe's order. The same ingredient can
/// appear on more than one line. <see cref="Grams"/> is the quantity's weight, calculated by Recipes from the ingredient's
/// density or piece weight, so a consumer can add up quantities of different dimensions without that unit data.
/// </summary>
public sealed record RecipeIngredientData(
    Guid IngredientId,
    string HungarianName,
    decimal Amount,
    IngredientUnit Unit,
    decimal Grams);

/// <summary>The units a recipe measures an ingredient in. Spoons and cups are metric: 5, 15 and 250 ml.</summary>
public enum IngredientUnit
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
