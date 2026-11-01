namespace Recipes.Domain;

/// <summary>A recipe's nutrition for the whole dish and for one of its servings. Unrounded: rounding is for display.</summary>
public sealed record RecipeNutrition(NutrientProfile Total, NutrientProfile PerServing)
{
    /// <summary>
    /// The calculation behind <see cref="Recipe.CalculateNutrition"/>, for callers that have a recipe's lines without
    /// the aggregate (a list projection): each line's quantity in grams times its ingredient's nutrients per 100 g.
    /// <paramref name="ingredients"/> must hold the <see cref="CatalogIngredient"/> of every line, keyed by id.
    /// </summary>
    public static RecipeNutrition Calculate(
        IEnumerable<(Guid IngredientId, Quantity Quantity)> lines,
        int servings,
        IReadOnlyDictionary<Guid, CatalogIngredient> ingredients)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(servings);
        ArgumentNullException.ThrowIfNull(ingredients);

        var total = NutrientProfile.Zero;
        foreach (var (ingredientId, quantity) in lines)
        {
            if (!ingredients.TryGetValue(ingredientId, out var ingredient))
            {
                throw new ArgumentException($"Ingredient {ingredientId} of the recipe is missing.", nameof(ingredients));
            }

            total += ingredient.NutrientsFor(quantity);
        }

        return new RecipeNutrition(total, total.Scale(1m / servings));
    }
}
