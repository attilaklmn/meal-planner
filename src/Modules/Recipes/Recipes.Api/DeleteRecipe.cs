using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recipes.Infrastructure.Persistence;

namespace Recipes.Api;

// DELETE /api/recipes/{id}
internal static class DeleteRecipe
{
    public static async Task<Results<NoContent, NotFound>> Handle(
        Guid id, RecipesDbContext dbContext, CancellationToken cancellationToken)
    {
        // One DELETE statement, no loading first; the database cascades to the ingredient lines.
        // Ingredient copies stay: other recipes may use them, and an unused copy costs one row.
        var deleted = await dbContext.Recipes.Where(r => r.Id == id).ExecuteDeleteAsync(cancellationToken);

        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }
}
