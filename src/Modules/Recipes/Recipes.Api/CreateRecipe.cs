using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Recipes.Infrastructure.Persistence;

namespace Recipes.Api;

// POST /api/recipes
internal static class CreateRecipe
{
    public static async Task<Results<CreatedAtRoute<RecipeDetails>, ValidationProblem>> Handle(
        RecipeRequest request,
        RecipeWriter writer,
        RecipesDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var result = await writer.WriteAsync(recipe: null, request, cancellationToken);
        if (result.Recipe is not { } recipe)
        {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }

        dbContext.Recipes.Add(recipe);
        // One transaction: the recipe and the ingredient copies it was validated against.
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.CreatedAtRoute(recipe.ToDetails(result.Ingredients), GetRecipe.Name, new { id = recipe.Id });
    }
}
