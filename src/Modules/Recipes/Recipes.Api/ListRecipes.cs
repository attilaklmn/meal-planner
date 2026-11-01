using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recipes.Domain;
using Recipes.Infrastructure.Persistence;

namespace Recipes.Api;

// GET /api/recipes?search=gulyas
internal static class ListRecipes
{
    public static async Task<Ok<RecipeSummary[]>> Handle(
        [Description("Part of the title; case and accents don't matter. Omit to list every recipe.")] string? search,
        RecipesDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Recipes.AsNoTracking();

        // Every word has to appear somewhere in the title, as in the ingredient search.
        var term = SearchText.Normalize(search ?? "");
        foreach (var word in term.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var contains = $"%{word}%";
            query = query.Where(r => EF.Functions.Like(r.SearchText, contains));
        }

        // Projection: only the columns the list needs (no steps document), lines as id + quantity.
        var rows = await query
            .OrderBy(r => r.Title)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Servings,
                r.Tags,
                Lines = r.Ingredients.Select(l => new { l.IngredientId, l.Quantity.Amount, l.Quantity.Unit }).ToList(),
            })
            .ToListAsync(cancellationToken);

        var ingredientIds = rows.SelectMany(r => r.Lines.Select(l => l.IngredientId)).Distinct().ToList();
        var ingredients = await dbContext.Ingredients
            .AsNoTracking()
            .Where(i => ingredientIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        // Nutrition is calculated in memory by the domain, not in SQL, so unit conversion stays in one place. Fine for a
        // household's recipes; a long list would store the per-serving value and refresh it when an ingredient changes.
        return TypedResults.Ok(rows
            .Select(r => new RecipeSummary(
                r.Id,
                r.Title,
                r.Servings,
                r.Tags,
                Math.Round(
                    RecipeNutrition.Calculate(
                        r.Lines.Select(l => (l.IngredientId, new Quantity(l.Amount, l.Unit))), r.Servings, ingredients)
                        .PerServing.EnergyKcal,
                    NutritionResponse.Decimals)))
            .ToArray());
    }
}
