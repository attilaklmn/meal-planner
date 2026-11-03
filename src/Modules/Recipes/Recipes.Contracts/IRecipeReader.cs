namespace Recipes.Contracts;

/// <summary>
/// Recipes' recipe data for other modules. In v1 an in-process call; Planning uses it to put recipes on the weekly plan
/// and to build the shopping list from them (ADR 0005).
/// </summary>
public interface IRecipeReader
{
    /// <summary>The recipes with the given ids; ids Recipes doesn't know are left out.</summary>
    Task<IReadOnlyList<RecipeData>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
