using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recipes.Infrastructure.Persistence;

namespace Recipes.Api;

// GET /api/recipes/{id}
internal static class GetRecipe
{
    public const string Name = "GetRecipe";

    public static async Task<Results<Ok<RecipeDetails>, NotFound>> Handle(
        Guid id, RecipesDbContext dbContext, CancellationToken cancellationToken)
    {
        // The aggregate itself, read-only: the details need the domain's nutrition calculation.
        // Split query: the recipe row and its lines in two queries, instead of one join repeating the recipe
        // (with its steps document) on every line.
        var recipe = await dbContext.Recipes
            .AsNoTracking()
            .Include(r => r.Ingredients)
            .AsSplitQuery()
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        var ingredientIds = recipe.Ingredients.Select(l => l.IngredientId).Distinct().ToList();
        var ingredients = await dbContext.Ingredients
            .AsNoTracking()
            .Where(i => ingredientIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        return TypedResults.Ok(recipe.ToDetails(ingredients));
    }
}
