namespace Catalog.Contracts;

/// <summary>
/// Catalog's ingredient data for other modules. In v1 an in-process call; Recipes uses it to refresh its copies
/// when a recipe is saved (ADR 0004).
/// </summary>
public interface ICatalogIngredients
{
    /// <summary>The ingredients with the given ids; ids Catalog doesn't know are left out.</summary>
    Task<IReadOnlyList<CatalogIngredientData>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
