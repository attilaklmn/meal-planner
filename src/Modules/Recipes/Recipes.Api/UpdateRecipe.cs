using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recipes.Infrastructure.Persistence;

namespace Recipes.Api;

// PUT /api/recipes/{id}: replaces the whole recipe, ingredient lines included.
internal static class UpdateRecipe
{
    public static async Task<Results<Ok<RecipeDetails>, NotFound, ValidationProblem>> Handle(
        Guid id,
        RecipeRequest request,
        RecipeWriter writer,
        RecipesDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes
            .Include(r => r.Ingredients)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        var result = await writer.WriteAsync(recipe, request, cancellationToken);
        if (result.Recipe is null)
        {
            // The tracked recipe may be half-changed in memory; it is never saved, and the DbContext ends with the request.
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(recipe.ToDetails(result.Ingredients));
    }
}
