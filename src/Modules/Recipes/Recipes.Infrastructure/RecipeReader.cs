using Microsoft.EntityFrameworkCore;
using Recipes.Contracts;
using Recipes.Domain;
using Recipes.Infrastructure.Persistence;

namespace Recipes.Infrastructure;

internal sealed class RecipeReader(RecipesDbContext dbContext) : IRecipeReader
{
    public async Task<IReadOnlyList<RecipeData>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);

        // Projection: no steps or tags, lines as id + quantity in the recipe's order.
        var rows = await dbContext.Recipes
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Select(r => new RecipeRow(
                r.Id,
                r.Title,
                r.Servings,
                r.Ingredients
                    .OrderBy(l => l.Position)
                    .Select(l => new LineRow(l.IngredientId, l.Quantity.Amount, l.Quantity.Unit))
                    .ToList()))
            .ToListAsync(cancellationToken);

        var ingredientIds = rows.SelectMany(r => r.Lines.Select(l => l.IngredientId)).Distinct().ToList();
        var ingredients = await dbContext.Ingredients
            .AsNoTracking()
            .Where(i => ingredientIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        return rows.Select(r => ToRecipeData(r, ingredients)).ToList();
    }

    /// <summary>Grams and nutrition come from the domain, so unit conversion stays in one place.</summary>
    internal static RecipeData ToRecipeData(RecipeRow row, IReadOnlyDictionary<Guid, CatalogIngredient> ingredients)
    {
        var lines = row.Lines.Select(l => (l.IngredientId, Quantity: new Quantity(l.Amount, l.Unit))).ToList();
        var nutrition = RecipeNutrition.Calculate(lines, row.Servings, ingredients);

        return new RecipeData(
            row.Id,
            row.Title,
            row.Servings,
            nutrition.PerServing.EnergyKcal,
            lines
                .Select(l =>
                {
                    var ingredient = ingredients[l.IngredientId];
                    return new RecipeIngredientData(
                        l.IngredientId,
                        ingredient.HungarianName,
                        l.Quantity.Amount,
                        ToIngredientUnit(l.Quantity.Unit),
                        l.Quantity.ToGrams(ingredient.UnitConversion));
                })
                .ToList());
    }

    // Explicit, not by name: renaming a domain unit must not silently change the public contract.
    internal static IngredientUnit ToIngredientUnit(Unit unit) => unit switch
    {
        Unit.Gram => IngredientUnit.Gram,
        Unit.Kilogram => IngredientUnit.Kilogram,
        Unit.Milliliter => IngredientUnit.Milliliter,
        Unit.Liter => IngredientUnit.Liter,
        Unit.Teaspoon => IngredientUnit.Teaspoon,
        Unit.Tablespoon => IngredientUnit.Tablespoon,
        Unit.Cup => IngredientUnit.Cup,
        Unit.Piece => IngredientUnit.Piece,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit."),
    };

    internal sealed record RecipeRow(Guid Id, string Title, int Servings, List<LineRow> Lines);

    internal sealed record LineRow(Guid IngredientId, decimal Amount, Unit Unit);
}
