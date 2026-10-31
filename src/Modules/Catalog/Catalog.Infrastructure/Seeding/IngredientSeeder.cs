using Catalog.Domain;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Seeding;

/// <summary>
/// Brings the catalog in line with the embedded seed file: adds missing ingredients, updates changed ones.
/// Rows are matched by (Source, SourceId), so running it again changes nothing.
/// Ingredients that disappear from the file are kept: recipes may already use them.
/// </summary>
internal sealed partial class IngredientSeeder(CatalogDbContext dbContext, ILogger<IngredientSeeder> logger)
{
    public const string Source = "usda";
    private const string ResourceName = "Catalog.Infrastructure.Seeding.ingredients.csv";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var rows = ReadEmbeddedRows();

        var existing = await dbContext.Ingredients
            .Where(i => i.Source == Source)
            .ToDictionaryAsync(i => i.SourceId, cancellationToken);

        var added = 0;
        foreach (var row in rows)
        {
            if (existing.TryGetValue(row.SourceId, out var ingredient))
            {
                // Same values in, no change tracked: EF compares against the loaded snapshot.
                ingredient.UpdateFromSource(row.Name, row.HungarianName, row.NutrientsPer100g, row.DensityGramsPerMl, row.PieceWeightGrams);
            }
            else
            {
                dbContext.Ingredients.Add(Ingredient.Create(
                    row.Name,
                    row.HungarianName,
                    row.NutrientsPer100g,
                    Source,
                    row.SourceId,
                    row.DensityGramsPerMl,
                    row.PieceWeightGrams));
                added++;
            }
        }

        var updated = dbContext.ChangeTracker.Entries<Ingredient>().Count(entry => entry.State == EntityState.Modified);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogSeeded(rows.Count, added, updated);
    }

    internal static IReadOnlyList<IngredientSeedRow> ReadEmbeddedRows()
    {
        using var stream = typeof(IngredientSeeder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);

        return IngredientCsv.Read(reader);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ingredient seed: {Rows} rows, {Added} added, {Updated} updated")]
    private partial void LogSeeded(int rows, int added, int updated);
}
