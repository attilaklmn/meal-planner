using System.ComponentModel;
using Catalog.Domain;
using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api;

// GET /api/catalog/ingredients?search=csirke&limit=20
internal static class SearchIngredients
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    public static async Task<Results<Ok<IngredientSummary[]>, ValidationProblem>> Handle(
        [Description("Part of the English or Hungarian name; case and accents don't matter.")] string search,
        [Description("Maximum number of results, 1 to 50 (default 20).")] int? limit,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Normalized the same way as Ingredient.SearchText. The result holds only letters, digits and spaces,
        // so it can't contain LIKE wildcards (% or _) and needs no escaping.
        var term = SearchText.Normalize(search);
        if (term.Length == 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(search)] = ["Enter at least one letter or digit."],
            });
        }

        var take = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

        var query = dbContext.Ingredients.AsNoTracking();

        // Every word has to appear somewhere, so "csirke mell" finds "Csirkemell".
        foreach (var word in term.Split(' '))
        {
            var contains = $"%{word}%";
            query = query.Where(i => EF.Functions.Like(i.SearchText, contains));
        }

        var startsWith = $"{term}%";
        var wordStartsWith = $"% {term}%";

        return TypedResults.Ok(await query
            // Best matches first: Hungarian name starts with the term, then any word does, then the rest.
            .OrderBy(i => EF.Functions.Like(i.SearchText, startsWith) ? 0
                : EF.Functions.Like(i.SearchText, wordStartsWith) ? 1
                : 2)
            // Among equals the shorter, more generic name ("Tojás" before "Tojássárgája").
            .ThenBy(i => i.HungarianName.Length)
            .ThenBy(i => i.HungarianName)
            .Take(take)
            // Projection: only the columns the response needs are read, no entities are materialized.
            .Select(i => new IngredientSummary(
                i.Id,
                i.Name,
                i.HungarianName,
                new NutrientsResponse(
                    i.NutrientsPer100g.EnergyKcal,
                    i.NutrientsPer100g.Protein,
                    i.NutrientsPer100g.Carbohydrate,
                    i.NutrientsPer100g.Fat,
                    i.NutrientsPer100g.Fiber)))
            .ToArrayAsync(cancellationToken));
    }
}
