using Catalog.Contracts;
using Microsoft.EntityFrameworkCore;
using Recipes.Domain;
using Recipes.Infrastructure.Persistence;

namespace Recipes.Api;

/// <summary>
/// Turns a <see cref="RecipeRequest"/> into a new or updated <see cref="Recipe"/>, shared by POST and PUT.
/// First it refreshes Recipes' copies of the ingredients the request uses from Catalog (ADR 0004), so the copies
/// are saved in the same transaction as the recipe. Nothing is saved here.
/// </summary>
internal sealed class RecipeWriter(RecipesDbContext dbContext, ICatalogIngredients catalog)
{
    /// <summary>Creates a recipe (<paramref name="recipe"/> null) or replaces an existing one's content.</summary>
    public async Task<RecipeWriteResult> WriteAsync(Recipe? recipe, RecipeRequest request, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();
        var ingredients = await RefreshIngredientCopiesAsync(request.Ingredients, errors, cancellationToken);

        if (recipe is null)
        {
            recipe = errors.Try(
                () => Recipe.Create(request.Title, request.Servings, request.Steps, request.Tags), RecipeField);
        }
        else
        {
            errors.Try(
                () =>
                {
                    recipe.Update(request.Title, request.Servings, request.Steps, request.Tags);
                    return recipe;
                },
                RecipeField);
        }

        // When the recipe itself is invalid, its lines are still checked, on a throwaway recipe, so the client gets
        // every error in one response and the line rules stay in the domain.
        var target = recipe ?? Recipe.Create("Validation only", 1, [], []);

        // PUT replaces the whole list, so lines get new ids; nothing refers to a line from outside the recipe.
        target.ClearIngredients();
        for (var index = 0; index < request.Ingredients.Count; index++)
        {
            AddLine(target, request.Ingredients[index], $"ingredients[{index}]", ingredients, errors);
        }

        return new RecipeWriteResult(errors.IsEmpty ? recipe : null, ingredients, errors);
    }

    private static void AddLine(
        Recipe recipe,
        RecipeIngredientRequest line,
        string field,
        Dictionary<Guid, CatalogIngredient> ingredients,
        ValidationErrors errors)
    {
        // An unknown ingredient is already reported.
        if (!ingredients.TryGetValue(line.IngredientId, out var ingredient))
        {
            return;
        }

        var quantity = errors.Try(() => new Quantity(line.Amount, line.Unit), parameter => $"{field}.{parameter}");
        if (quantity is not null)
        {
            // AddIngredient reports a unit the ingredient can't be converted from as "quantity": that's the unit's fault.
            errors.Try(
                () => recipe.AddIngredient(ingredient, quantity, line.Note),
                parameter => $"{field}.{(parameter == "quantity" ? "unit" : parameter)}");
        }
    }

    private async Task<Dictionary<Guid, CatalogIngredient>> RefreshIngredientCopiesAsync(
        IReadOnlyList<RecipeIngredientRequest> lines, ValidationErrors errors, CancellationToken cancellationToken)
    {
        var ids = lines.Select(l => l.IngredientId).Distinct().ToList();
        var fromCatalog = (await catalog.GetByIdsAsync(ids, cancellationToken)).ToDictionary(i => i.Id);
        var copies = await dbContext.Ingredients
            .Where(i => ids.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        for (var index = 0; index < lines.Count; index++)
        {
            if (!fromCatalog.ContainsKey(lines[index].IngredientId))
            {
                errors.Add($"ingredients[{index}].ingredientId", "No ingredient with this id in the catalog.");
            }
        }

        var result = new Dictionary<Guid, CatalogIngredient>();
        foreach (var data in fromCatalog.Values)
        {
            var nutrients = new NutrientProfile(
                data.EnergyKcalPer100g, data.ProteinPer100g, data.CarbohydratePer100g, data.FatPer100g, data.FiberPer100g);
            var conversion = new UnitConversion(data.DensityGramsPerMl, data.PieceWeightGrams);

            if (copies.TryGetValue(data.Id, out var copy))
            {
                copy.RefreshFrom(data.Name, data.HungarianName, nutrients, conversion);
            }
            else
            {
                copy = CatalogIngredient.Create(data.Id, data.Name, data.HungarianName, nutrients, conversion);
                dbContext.Ingredients.Add(copy);
            }

            result[data.Id] = copy;
        }

        return result;
    }

    // RecipeStep reports a bad step as "text"; to the client it's one of the steps.
    private static string RecipeField(string? parameter) => parameter == "text" ? "steps" : parameter ?? "recipe";
}

/// <summary><see cref="Recipe"/> is null when <see cref="Errors"/> has any.</summary>
internal sealed record RecipeWriteResult(
    Recipe? Recipe, IReadOnlyDictionary<Guid, CatalogIngredient> Ingredients, ValidationErrors Errors);
