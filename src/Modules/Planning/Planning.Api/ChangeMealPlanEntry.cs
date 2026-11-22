using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Planning.Domain;
using Planning.Infrastructure.Persistence;
using Recipes.Contracts;

namespace Planning.Api;

// PUT /api/plans/2026-11-02/entries/{entryId}
internal static class ChangeMealPlanEntry
{
    public static async Task<Results<Ok<MealPlanResponse>, NotFound, ValidationProblem>> Handle(
        [Description("The Monday the week starts on, as yyyy-MM-dd.")] DateOnly weekStart,
        Guid entryId,
        ChangeMealPlanEntryRequest request,
        IRecipeReader recipes,
        PlanningDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.MealPlans
            .Include(p => p.Entries)
            .SingleOrDefaultAsync(p => p.WeekStart == weekStart, cancellationToken);
        var entry = plan?.Entries.SingleOrDefault(e => e.Id == entryId);
        if (plan is null || entry is null)
        {
            return TypedResults.NotFound();
        }

        var errors = new ValidationErrors();
        if (errors.Try(() => plan.ChangeEntry(entryId, request.Day, request.Meal, request.Servings), p => p ?? "entry") is null)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        // A change is when the copy catches up with the recipe (ADR 0005); a deleted recipe keeps its last copy.
        var recipe = (await recipes.GetByIdsAsync([entry.Recipe.RecipeId], cancellationToken)).SingleOrDefault();
        if (recipe is not null)
        {
            plan.RefreshRecipe(new RecipeSnapshot(recipe.Id, recipe.Title, recipe.EnergyKcalPerServing));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(plan.ToResponse());
    }
}
