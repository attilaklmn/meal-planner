using Recipes.Domain;

namespace Recipes.Api;

internal static class RecipeDetailsMapping
{
    /// <summary><paramref name="ingredients"/> must hold the copy of every ingredient the recipe uses.</summary>
    public static RecipeDetails ToDetails(this Recipe recipe, IReadOnlyDictionary<Guid, CatalogIngredient> ingredients)
    {
        var nutrition = recipe.CalculateNutrition(ingredients);

        return new RecipeDetails(
            recipe.Id,
            recipe.Title,
            recipe.Servings,
            [.. recipe.Steps.Select(s => s.Text)],
            recipe.Tags,
            // EF Core loads the lines in no particular order.
            [.. recipe.Ingredients.OrderBy(l => l.Position).Select(l => new RecipeIngredientResponse(
                l.Id,
                l.IngredientId,
                ingredients[l.IngredientId].Name,
                ingredients[l.IngredientId].HungarianName,
                l.Quantity.Amount,
                l.Quantity.Unit,
                l.Note))],
            NutritionResponse.From(nutrition.PerServing),
            NutritionResponse.From(nutrition.Total));
    }
}
