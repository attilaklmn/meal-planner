using Catalog.Contracts;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

internal sealed class CatalogIngredients(CatalogDbContext dbContext) : ICatalogIngredients
{
    public async Task<IReadOnlyList<CatalogIngredientData>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);

        return await dbContext.Ingredients
            .AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .Select(i => new CatalogIngredientData(
                i.Id,
                i.Name,
                i.HungarianName,
                i.NutrientsPer100g.EnergyKcal,
                i.NutrientsPer100g.Protein,
                i.NutrientsPer100g.Carbohydrate,
                i.NutrientsPer100g.Fat,
                i.NutrientsPer100g.Fiber,
                i.DensityGramsPerMl,
                i.PieceWeightGrams))
            .ToListAsync(cancellationToken);
    }
}
