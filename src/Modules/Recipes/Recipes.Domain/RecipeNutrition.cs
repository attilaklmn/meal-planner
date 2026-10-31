namespace Recipes.Domain;

/// <summary>A recipe's nutrition for the whole dish and for one of its servings. Unrounded: rounding is for display.</summary>
public sealed record RecipeNutrition(NutrientProfile Total, NutrientProfile PerServing);
